using Bookstore3.Model;
using Bookstore3.Model.Abstract;
using Bookstore3.Repository;
using Config.Net;
using KpzRepository.Repository;
using Syncfusion.UI.Xaml.Grid;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Bookstore3.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        BooksDataGrid.SelectionChanged += BooksDataGrid_SelectionChanged;
        BooksDataGrid.SortColumnsChanging += BooksDataGrid_SortColumnsChanging;
        LocationChanged += (_, _) => PositionColumnChooserIfVisible();
        SizeChanged += (_, _) => PositionColumnChooserIfVisible();
    }

    private void MainWindow_LoadedHandler(object sender, RoutedEventArgs e)
    {
        try
        {
            _appSettings = new ConfigurationBuilder<IAppSettings>()
                .UseJsonFile(AppConstants.AppSettingsFileName)
                .Build();

            _repositoryFactory = new BookstoreRepositoryFactory(_appSettings.DefaultConnectionString);
            _bookRepository = _repositoryFactory.GetBookRepository();
            _groupRepository = _repositoryFactory.GetBaseRepository<long, group>();
            _publisherRepository = _repositoryFactory.GetBaseRepository<long, publisher>();
            _shopRepository = _repositoryFactory.GetBaseRepository<long, shop>();
            _languageRepository = _repositoryFactory.GetBaseRepository<long, language>();
            _cityRepository = _repositoryFactory.GetBaseRepository<long, city>();

            LoadBooks();
            UpdateStatusBar();
            InitializeColumnChooser();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"An error occurred: {ex.Message}");
        }
    }

    private void RefreshDataButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        LoadBooks();
        UpdateStatusBar();
    }

    private void GroupsMenuItem_ClickHandler(object sender, RoutedEventArgs e) =>
        ShowLookupWindow(_groupRepository, "Groups");

    private void PublishersMenuItem_ClickHandler(object sender, RoutedEventArgs e) =>
        ShowLookupWindow(_publisherRepository, "Publishers");

    private void ShopsMenuItem_ClickHandler(object sender, RoutedEventArgs e) =>
        ShowLookupWindow(_shopRepository, "Shops");

    private void LanguagesMenuItem_ClickHandler(object sender, RoutedEventArgs e) =>
        ShowLookupWindow(_languageRepository, "Languages");

    private void CitiesMenuItem_ClickHandler(object sender, RoutedEventArgs e) =>
        ShowLookupWindow(_cityRepository, "Cities");

    private void ShowLookupWindow<TEntity>(IKpzRepository<long, TEntity>? repository, string title)
        where TEntity : lookup_entity, new()
    {
        if (repository is null)
            return;

        var lookupWindow = new BaseLookupWindow<TEntity>(repository, title)
        {
            Owner = this
        };
        lookupWindow.ShowDialog();
        LoadBooks();
        UpdateStatusBar();
    }

    private void ColumnChooserButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        if (_columnChooserWindow is null)
            InitializeColumnChooser();

        PositionColumnChooser();
        _columnChooserWindow!.Show();
        _columnChooserWindow.Activate();
    }

    private void InitializeColumnChooser()
    {
        if (_columnChooserWindow is not null)
            return;

        _columnChooserWindow = new ColumnChooser(BooksDataGrid)
        {
            Title = "Columns",
            WaterMarkText = "Drag columns here to hide them",
            Owner = this,
            Width = 280,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowInTaskbar = false
        };

        BooksDataGrid.GridColumnDragDropController =
            new GridColumnChooserController(BooksDataGrid, _columnChooserWindow);

        PositionColumnChooser();
    }

    private void PositionColumnChooserIfVisible()
    {
        if (_columnChooserWindow?.IsVisible == true)
            PositionColumnChooser();
    }

    private void PositionColumnChooser()
    {
        if (_columnChooserWindow is null)
            return;

        const double margin = 8;
        _columnChooserWindow.Left = Left + ActualWidth - _columnChooserWindow.Width - margin;
        _columnChooserWindow.Top = Top + ActualHeight - _columnChooserWindow.Height - margin;
    }

    private void LoadBooks()
    {
        if (_bookRepository is null)
            return;

        var books = _bookRepository.GetAllBooksLightweight().ToList();
        _allBooksCollection = new ObservableCollection<book_ex>(books);
        BooksDataGrid.ItemsSource = _allBooksCollection;
        ApplyDefaultGridSort();

        if (_allBooksCollection.Count > 0)
        {
            _currentBookIndex = 0;
            DisplayCurrentBook();
        }
        else
        {
            _currentBookIndex = -1;
            ClearDetails();
        }

        UpdateDetailNavigationButtons();
    }

    private void ApplyDefaultGridSort()
    {
        BooksDataGrid.SortColumnDescriptions.Clear();
        BooksDataGrid.SortColumnDescriptions.Add(new SortColumnDescription
        {
            ColumnName = nameof(book_ex.id),
            SortDirection = ListSortDirection.Ascending
        });
    }

    private void BooksDataGrid_SortColumnsChanging(object? sender, GridSortColumnsChangingEventArgs e)
    {
        // Accumulate sort columns without Ctrl so ShowSortNumbers can display 1, 2, 3…
        e.Cancel = true;

        BooksDataGrid.Dispatcher.BeginInvoke(() =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add when e.AddedItems.Count > 0:
                    BooksDataGrid.SortColumnDescriptions.Add(e.AddedItems[0]);
                    break;

                case NotifyCollectionChangedAction.Replace when e.AddedItems.Count > 0:
                {
                    var columnName = e.AddedItems[0].ColumnName;
                    var existing = BooksDataGrid.SortColumnDescriptions
                        .FirstOrDefault(sd => sd.ColumnName == columnName);
                    if (existing is not null)
                        BooksDataGrid.SortColumnDescriptions.Remove(existing);
                    BooksDataGrid.SortColumnDescriptions.Add(e.AddedItems[0]);
                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                    foreach (SortColumnDescription removed in e.RemovedItems)
                    {
                        var existing = BooksDataGrid.SortColumnDescriptions
                            .FirstOrDefault(sd => sd.ColumnName == removed.ColumnName);
                        if (existing is not null)
                            BooksDataGrid.SortColumnDescriptions.Remove(existing);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    BooksDataGrid.SortColumnDescriptions.Clear();
                    break;
            }
        }, DispatcherPriority.ApplicationIdle);
    }

    private void BooksDataGrid_SelectionChanged(object? sender, GridSelectionChangedEventArgs e)
    {
        if (_allBooksCollection is null || BooksDataGrid.SelectedItem is not book_ex selected)
            return;

        var index = FindBookIndexById(selected.id);
        if (index < 0)
            return;

        _currentBookIndex = index;
        DisplayCurrentBook();
        UpdateDetailNavigationButtons();
    }

    private void DetailFirstButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        NavigateToBookId(_bookRepository?.GetMinId());
    }

    private void DetailPreviousButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        if (_allBooksCollection is null || _currentBookIndex <= 0)
            return;

        _currentBookIndex--;
        DisplayCurrentBook();
        SyncGridSelectionToCurrentBook();
        UpdateDetailNavigationButtons();
    }

    private void DetailNextButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        if (_allBooksCollection is null || _currentBookIndex < 0 || _currentBookIndex >= _allBooksCollection.Count - 1)
            return;

        _currentBookIndex++;
        DisplayCurrentBook();
        SyncGridSelectionToCurrentBook();
        UpdateDetailNavigationButtons();
    }

    private void DetailLastButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        NavigateToBookId(_bookRepository?.GetMaxId());
    }

    private void NavigateToBookId(long? bookId)
    {
        if (_allBooksCollection is null || _allBooksCollection.Count == 0 || bookId is null)
            return;

        var index = FindBookIndexById(bookId.Value);
        if (index < 0)
            return;

        _currentBookIndex = index;
        DisplayCurrentBook();
        SyncGridSelectionToCurrentBook();
        UpdateDetailNavigationButtons();
    }

    private void DisplayCurrentBook()
    {
        if (_allBooksCollection is null || _currentBookIndex < 0 || _currentBookIndex >= _allBooksCollection.Count)
        {
            ClearDetails();
            return;
        }

        var summary = _allBooksCollection[_currentBookIndex];
        book? full = _bookRepository?.Get(summary.id);

        DetailIdText.Text = summary.id.ToString(CultureInfo.InvariantCulture);
        DetailCreatedAtText.Text = AppUtils.FormatDateTime(summary.crt_date_time);
        DetailAuthorText.Text = summary.author ?? string.Empty;
        DetailTitleText.Text = summary.title;
        DetailGroupText.Text = summary.group_name ?? string.Empty;
        DetailPublisherText.Text = summary.publisher_name ?? string.Empty;
        DetailShopText.Text = summary.shop_name ?? string.Empty;
        DetailFormatText.Text = summary.format ?? string.Empty;
        DetailIsbnText.Text = summary.isbn ?? string.Empty;
        DetailPageCountText.Text = summary.page_count?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        DetailPriceText.Text = summary.price?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        DetailPublishYearText.Text = summary.publish_year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        DetailGotAtText.Text = AppUtils.FormatDateTime(summary.date_when_get);
        DetailHardcoverText.Text = summary.wrapper ? "Yes" : "No";
        DetailLanguageText.Text = summary.language_name ?? string.Empty;
        DetailDigitCopyText.Text = summary.has_digit_copy ? "Yes" : "No";
        DetailEditionText.Text = summary.edition.ToString(CultureInfo.InvariantCulture);
        DetailCityText.Text = summary.city_name ?? string.Empty;
        DetailBookFileText.Text = summary.book_file ?? string.Empty;

        DetailAnnotationText.Text = full?.annotation ?? string.Empty;
        DetailDetailsText.Text = full?.details ?? string.Empty;

        LoadCoverImage(full?.cover_image);
    }

    private void ClearDetails()
    {
        DetailIdText.Text = string.Empty;
        DetailCreatedAtText.Text = string.Empty;
        DetailAuthorText.Text = string.Empty;
        DetailTitleText.Text = string.Empty;
        DetailGroupText.Text = string.Empty;
        DetailPublisherText.Text = string.Empty;
        DetailShopText.Text = string.Empty;
        DetailFormatText.Text = string.Empty;
        DetailIsbnText.Text = string.Empty;
        DetailPageCountText.Text = string.Empty;
        DetailPriceText.Text = string.Empty;
        DetailPublishYearText.Text = string.Empty;
        DetailGotAtText.Text = string.Empty;
        DetailHardcoverText.Text = string.Empty;
        DetailLanguageText.Text = string.Empty;
        DetailDigitCopyText.Text = string.Empty;
        DetailEditionText.Text = string.Empty;
        DetailCityText.Text = string.Empty;
        DetailBookFileText.Text = string.Empty;
        DetailAnnotationText.Text = string.Empty;
        DetailDetailsText.Text = string.Empty;
        LoadCoverImage(null);
    }

    private void LoadCoverImage(byte[]? coverImage)
    {
        if (coverImage is { Length: > 0 })
        {
            using var stream = new MemoryStream(coverImage);
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            CoverImage.Source = image;
            CoverImage.Visibility = Visibility.Visible;
            NoImagePanel.Visibility = Visibility.Collapsed;
            return;
        }

        CoverImage.Source = null;
        CoverImage.Visibility = Visibility.Collapsed;
        NoImagePanel.Visibility = Visibility.Visible;
    }

    private void UpdateDetailNavigationButtons()
    {
        var hasBooks = _allBooksCollection is { Count: > 0 } && _currentBookIndex >= 0;

        if (!hasBooks || _bookRepository is null)
        {
            DetailFirstButton.IsEnabled = false;
            DetailPreviousButton.IsEnabled = false;
            DetailNextButton.IsEnabled = false;
            DetailLastButton.IsEnabled = false;
            return;
        }

        var currentId = _allBooksCollection![_currentBookIndex].id;
        var minId = _bookRepository.GetMinId();
        var maxId = _bookRepository.GetMaxId();

        DetailFirstButton.IsEnabled = currentId != minId;
        DetailPreviousButton.IsEnabled = _currentBookIndex > 0;
        DetailNextButton.IsEnabled = _currentBookIndex < _allBooksCollection.Count - 1;
        DetailLastButton.IsEnabled = currentId != maxId;
    }

    private void SyncGridSelectionToCurrentBook()
    {
        if (_allBooksCollection is null || _currentBookIndex < 0 || _currentBookIndex >= _allBooksCollection.Count)
            return;

        BooksDataGrid.SelectedItem = _allBooksCollection[_currentBookIndex];
    }

    private int FindBookIndexById(long bookId)
    {
        if (_allBooksCollection is null)
            return -1;

        for (var i = 0; i < _allBooksCollection.Count; i++)
        {
            if (_allBooksCollection[i].id == bookId)
                return i;
        }

        return -1;
    }

    private void UpdateStatusBar()
    {
        if (_bookRepository is not null)
            TotalBookCountText.Text = string.Format("Total Book Count {0}", _bookRepository.Count());

        if (_groupRepository is not null)
            TotalGroupCountText.Text = string.Format("Total Group Count {0}", _groupRepository.Count());

        if (_shopRepository is not null)
            TotalShopCountText.Text = string.Format("Total Shop Count {0}", _shopRepository.Count());
    }

    private IAppSettings? _appSettings;
    private IBookstoreRepositoryFactory? _repositoryFactory;
    private IBookRepository? _bookRepository;
    private IKpzRepository<long, group>? _groupRepository;
    private IKpzRepository<long, publisher>? _publisherRepository;
    private IKpzRepository<long, shop>? _shopRepository;
    private IKpzRepository<long, language>? _languageRepository;
    private IKpzRepository<long, city>? _cityRepository;
    private ObservableCollection<book_ex>? _allBooksCollection;
    private int _currentBookIndex = -1;
    private ColumnChooser? _columnChooserWindow;
}
