using Bookstore3.Model;
using Bookstore3.Repository;
using Config.Net;
using KpzRepository.Repository;
using Syncfusion.UI.Xaml.Grid;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Bookstore3.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        BooksDataGrid.SelectionChanged += BooksDataGrid_SelectionChanged;
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
            _shopRepository = _repositoryFactory.GetBaseRepository<long, shop>();

            LoadBooks();
            UpdateStatusBar();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshDataButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        LoadBooks();
        UpdateStatusBar();
    }

    private void LoadBooks()
    {
        if (_bookRepository is null)
            return;

        var books = _bookRepository.GetAllBooksLightweight().ToList();
        _books = new ObservableCollection<book_ex>(books);
        BooksDataGrid.ItemsSource = _books;

        if (_books.Count > 0)
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

    private void BooksDataGrid_SelectionChanged(object? sender, GridSelectionChangedEventArgs e)
    {
        if (_books is null || BooksDataGrid.SelectedItem is not book_ex selected)
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
        if (_books is null || _currentBookIndex <= 0)
            return;

        _currentBookIndex--;
        DisplayCurrentBook();
        SyncGridSelectionToCurrentBook();
        UpdateDetailNavigationButtons();
    }

    private void DetailNextButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        if (_books is null || _currentBookIndex < 0 || _currentBookIndex >= _books.Count - 1)
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
        if (_books is null || _books.Count == 0 || bookId is null)
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
        if (_books is null || _currentBookIndex < 0 || _currentBookIndex >= _books.Count)
        {
            ClearDetails();
            return;
        }

        var summary = _books[_currentBookIndex];
        book? full = _bookRepository?.Get(summary.id);

        DetailIdText.Text = summary.id.ToString(CultureInfo.InvariantCulture);
        DetailCreatedAtText.Text = AppConstants.FormatDateTime(summary.crt_date_time);
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
        DetailGotAtText.Text = AppConstants.FormatDateTime(summary.date_when_get);
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
        var hasBooks = _books is { Count: > 0 } && _currentBookIndex >= 0;

        if (!hasBooks || _bookRepository is null)
        {
            DetailFirstButton.IsEnabled = false;
            DetailPreviousButton.IsEnabled = false;
            DetailNextButton.IsEnabled = false;
            DetailLastButton.IsEnabled = false;
            return;
        }

        var currentId = _books![_currentBookIndex].id;
        var minId = _bookRepository.GetMinId();
        var maxId = _bookRepository.GetMaxId();

        DetailFirstButton.IsEnabled = currentId != minId;
        DetailPreviousButton.IsEnabled = _currentBookIndex > 0;
        DetailNextButton.IsEnabled = _currentBookIndex < _books.Count - 1;
        DetailLastButton.IsEnabled = currentId != maxId;
    }

    private void SyncGridSelectionToCurrentBook()
    {
        if (_books is null || _currentBookIndex < 0 || _currentBookIndex >= _books.Count)
            return;

        BooksDataGrid.SelectedItem = _books[_currentBookIndex];
    }

    private int FindBookIndexById(long bookId)
    {
        if (_books is null)
            return -1;

        for (var i = 0; i < _books.Count; i++)
        {
            if (_books[i].id == bookId)
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
    private IKpzRepository<long, shop>? _shopRepository;
    private ObservableCollection<book_ex>? _books;
    private int _currentBookIndex = -1;
}
