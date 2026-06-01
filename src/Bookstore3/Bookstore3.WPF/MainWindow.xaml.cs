using Bookstore3.Model;
using Bookstore3.Model.Abstract;
using Bookstore3.Repository;
using Config.Net;
using KpzRepository.Repository;
using Microsoft.Win32;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Converter;
using Syncfusion.UI.Xaml.Grid.Helpers;
using Syncfusion.UI.Xaml.ScrollAxis;
using Syncfusion.XlsIO;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Bookstore3.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        BooksDataGrid.SelectionChanged += BooksDataGrid_SelectionChanged;
        BooksDataGrid.SortColumnsChanging += BooksDataGrid_SortColumnsChanging;
        BooksDataGrid.SortColumnsChanged += BooksDataGrid_SortColumnsChangedHandler;
        BooksDataGrid.FilterChanged += BooksDataGrid_FilterChangedHandler;
        BooksDataGrid.GroupExpanded += BooksDataGrid_GroupVisibilityChangedHandler;
        BooksDataGrid.GroupCollapsed += BooksDataGrid_GroupVisibilityChangedHandler;
        BooksDataGrid.CellDoubleTapped += BooksDataGrid_CellDoubleTappedHandler;
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
            FocusBooksDataGrid();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"An error occurred: {ex.Message}");
        }
    }

    private void FocusBooksDataGrid()
    {
        if (_allBooksCollection is { Count: > 0 })
            SyncGridSelectionToCurrentBook();

        BooksDataGrid.Dispatcher.BeginInvoke(() => BooksDataGrid.Focus(), DispatcherPriority.Loaded);
    }

    private void RefreshDataButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        var selectedBookId = GetSelectedBookId();
        LoadBooks();
        if (selectedBookId != AppConstants.NullRecordId)
            NavigateToBookId(selectedBookId);
        FocusBooksDataGrid();
        UpdateStatusBar();
    }

    private void NewRecordButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        if (_repositoryFactory is null)
            return;

        var bookWindow = new BookWindow(_repositoryFactory, AppConstants.NullRecordId)
        {
            Owner = this
        };
        if (bookWindow.ShowDialog() == true)
        {
            LoadBooks();
            NavigateToBookId(bookWindow.SavedBookId);
            UpdateStatusBar();
        }
    }

    private void RecordEditButton_ClickHandler(object sender, RoutedEventArgs e) =>
        EditSelectedBook();

    private void BooksDataGrid_CellDoubleTappedHandler(object? sender, GridCellDoubleTappedEventArgs e)
    {
        if (e.Record is not book_ex book)
            return;

        EditBook(book.id);
    }

    private void EditSelectedBook()
    {
        var bookId = GetSelectedBookId();
        if (bookId == AppConstants.NullRecordId)
        {
            AppUtils.ShowInfoMessage("Please select a book to edit.");
            return;
        }

        EditBook(bookId);
    }

    private void EditBook(long bookId)
    {
        if (_repositoryFactory is null)
            return;

        var bookWindow = new BookWindow(_repositoryFactory, bookId)
        {
            Owner = this
        };
        if (bookWindow.ShowDialog() == true)
        {
            LoadBooks();
            NavigateToBookId(bookWindow.SavedBookId);
            UpdateStatusBar();
        }
    }

    private void DeleteRecordButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        if (_bookRepository is null)
            return;

        if (BooksDataGrid.SelectedItem is not book_ex selected)
        {
            AppUtils.ShowInfoMessage("Please select a book to delete.");
            return;
        }

        var confirmResult = AppUtils.ShowConfirmMessage($"Delete book '{selected.title}'?", "Confirm Delete");
        if (confirmResult == false)
            return;

        _bookRepository.Delete(selected.id);
        LoadBooks();
        NavigateToBookId(_bookRepository.GetMaxId());
        UpdateStatusBar();
        AppUtils.ShowInfoMessage("Book deleted successfully.");
    }

    private long GetSelectedBookId()
    {
        if (BooksDataGrid.SelectedItem is book_ex selected)
            return selected.id;

        return AppConstants.NullRecordId;
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

    private void ExportToExcelMenuItem_ClickHandler(object sender, RoutedEventArgs e) =>
        RunGridExport(ExportFormat.Excel);

    private void ExportToCsvMenuItem_ClickHandler(object sender, RoutedEventArgs e) =>
        RunGridExport(ExportFormat.Csv);

    private void ExportToPdfMenuItem_ClickHandler(object sender, RoutedEventArgs e) =>
        RunGridExport(ExportFormat.Pdf);

    private void RunGridExport(ExportFormat format)
    {
        const string confirmMessage =
            "The selected rows will be exported.\r\n\r\n" +
            "• Ctrl+A — select all rows\r\n" +
            "• Shift+click — select a range of rows\r\n" +
            "• Ctrl+click — select individual rows\r\n\r\n" +
            "Do you want to continue?";

        if (AppUtils.ShowConfirmMessage(confirmMessage, "Confirm Export") == false)
            return;

        if (BooksDataGrid.SelectedItems.Count == 0)
        {
            AppUtils.ShowInfoMessage("Please select at least one row to export.");
            return;
        }

        var (filter, defaultExtension) = format switch
        {
            ExportFormat.Excel => ("Excel Files (*.xlsx)|*.xlsx", ".xlsx"),
            ExportFormat.Csv => ("CSV Files (*.csv)|*.csv", ".csv"),
            ExportFormat.Pdf => ("PDF Files (*.pdf)|*.pdf", ".pdf"),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        var dialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExtension.TrimStart('.'),
            FileName = BuildExportDefaultFileName(defaultExtension),
            AddExtension = true
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            switch (format)
            {
                case ExportFormat.Excel:
                    ExportSelectedRowsToExcel(dialog.FileName);
                    break;
                case ExportFormat.Csv:
                    ExportSelectedRowsToCsv(dialog.FileName);
                    break;
                case ExportFormat.Pdf:
                    ExportSelectedRowsToPdf(dialog.FileName);
                    break;
            }

            AppUtils.ShowInfoMessage("Export completed successfully.");
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Export failed: {ex.Message}");
        }
    }

    private static string BuildExportDefaultFileName(string extension)
    {
        var timestamp = DateTime.Now
            .ToString(AppConstants.ExportFileNameDateTimeFormat, CultureInfo.InvariantCulture);
        return $"Books_list_export_{timestamp}{extension}";
    }

    private void ExportSelectedRowsToExcel(string filePath)
    {
        var options = new ExcelExportingOptions
        {
            ExcelVersion = ExcelVersion.Excel2016
        };

        using var excelEngine = BooksDataGrid.ExportToExcel(BooksDataGrid.SelectedItems, options);
        excelEngine.Excel.Workbooks[0].SaveAs(filePath);
    }

    private void ExportSelectedRowsToCsv(string filePath)
    {
        var options = new ExcelExportingOptions
        {
            ExcelVersion = ExcelVersion.Excel2016
        };

        using var excelEngine = BooksDataGrid.ExportToExcel(BooksDataGrid.SelectedItems, options);
        excelEngine.Excel.Workbooks[0].SaveAs(filePath, ",");
    }

    private void ExportSelectedRowsToPdf(string filePath)
    {
        var options = new PdfExportingOptions
        {
            ExportGroups = false,
            ExportGroupSummary = false,
            ExportTableSummary = true
        };

        using var document = new PdfDocument();
        document.PageSettings.Orientation = PdfPageOrientation.Landscape;
        var page = document.Pages.Add();
        var pdfGrid = BooksDataGrid.ExportToPdfGrid(BooksDataGrid.SelectedItems, options);
        var format = new PdfGridLayoutFormat
        {
            Layout = PdfLayoutType.Paginate,
            Break = PdfLayoutBreakType.FitPage
        };
        pdfGrid.Draw(page, new PointF(0, 0), format);
        document.Save(filePath);
        document.Close(true);
    }

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

    private void SearchButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        if (IsSearchBarVisible)
            HideSearchBar();
        else
            ShowSearchBar();
    }

    private void SearchBarCloseButton_ClickHandler(object sender, RoutedEventArgs e) =>
        HideSearchBar();

    private void RunTheSearchButton_ClickHandler(object sender, RoutedEventArgs e) =>
        RunTheSearch();

    private void RunTheSearch()
    {
        if (_bookRepository is null || !IsSearchBarVisible)
            return;

        CloseSearchNothingFoundToolTip();

        var results = _bookRepository.SearchBooksLightweight(SearchTextBox.Text).ToList();
        if (results.Count == 0)
        {
            ApplySearchResultsToGrid(results, syncGridSelection: MainTabControl.SelectedIndex == 0);
            ShowSearchNothingFoundToolTip();
            return;
        }

        ApplySearchResultsToGrid(results, syncGridSelection: MainTabControl.SelectedIndex == 0);
    }

    private void ApplySearchResultsToGrid(IReadOnlyList<book_ex> books, bool syncGridSelection)
    {
        _allBooksCollection = new ObservableCollection<book_ex>(books);
        BooksDataGrid.ItemsSource = _allBooksCollection;
        ApplyDefaultGridSort();

        if (books.Count > 0)
        {
            _currentBookIndex = 0;
            DisplayCurrentBook();
            if (syncGridSelection)
                NavigateToVisibleBook(books[0]);
            else
                BooksDataGrid.SelectedItem = books[0];
        }
        else
        {
            _currentBookIndex = -1;
            BooksDataGrid.SelectedItem = null;
            ClearDetails();
        }

        UpdateDetailNavigationButtons();
    }

    private void ShowSearchNothingFoundToolTip()
    {
        _searchNothingFoundToolTip ??= new ToolTip
        {
            Content = "Nothing Found",
            Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
            StaysOpen = false
        };

        _searchNothingFoundToolTip.PlacementTarget = SearchBarBorder;
        _searchNothingFoundToolTip.IsOpen = true;
    }

    private void CloseSearchNothingFoundToolTip()
    {
        if (_searchNothingFoundToolTip?.IsOpen == true)
            _searchNothingFoundToolTip.IsOpen = false;
    }

    private void SearchTextBox_KeyDownHandler(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            RunTheSearch();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            HideSearchBar();
            e.Handled = true;
        }
    }

    private void MainWindow_PreviewKeyDownHandler(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsSearchBarVisible)
        {
            HideSearchBar();
            e.Handled = true;
        }
    }

    private void MainWindow_PreviewTextInputHandler(object sender, TextCompositionEventArgs e)
    {
        if ((ShouldCaptureSearchInput() == false) || string.IsNullOrEmpty(e.Text))
            return;

        if (IsSearchBarVisible == false)
        {
            ShowSearchBar();
            AppendToSearchTextBox(e.Text);
            e.Handled = true;
            return;
        }

        if (SearchTextBox.IsKeyboardFocusWithin == false)
        {
            SearchTextBox.Focus();
            AppendToSearchTextBox(e.Text);
            e.Handled = true;
        }
    }

    private bool ShouldCaptureSearchInput() =>
        MainTabControl.SelectedIndex is 0 or 1;

    private bool IsSearchBarVisible => SearchBarBorder.Visibility == Visibility.Visible;

    private void ShowSearchBar()
    {
        SearchBarBorder.Visibility = Visibility.Visible;
        SearchTextBox.Focus();
        SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
    }

    private void HideSearchBar()
    {
        CloseSearchNothingFoundToolTip();
        SearchBarBorder.Visibility = Visibility.Collapsed;
        LoadBooks();
        FocusBooksDataGrid();
    }

    private void AppendToSearchTextBox(string text)
    {
        SearchTextBox.Focus();
        SearchTextBox.Text += text;
        SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
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

            UpdateDetailNavigationButtons();
        }, DispatcherPriority.ApplicationIdle);
    }

    private void BooksDataGrid_SortColumnsChangedHandler(object? sender, GridSortColumnsChangedEventArgs e) =>
        UpdateDetailNavigationButtons();

    private void BooksDataGrid_FilterChangedHandler(object? sender, GridFilterEventArgs e)
    {
        BooksDataGrid.Dispatcher.BeginInvoke(SyncVisibleGridSelection, DispatcherPriority.Loaded);
    }

    private void BooksDataGrid_GroupVisibilityChangedHandler(object? sender, GroupChangedEventArgs e) =>
        UpdateDetailNavigationButtons();

    private void SyncVisibleGridSelection()
    {
        if (BooksDataGrid.SelectedItem is book_ex selected &&
            BooksDataGrid.ResolveToRowIndex(selected) >= 0)
        {
            _currentBookIndex = FindBookIndexById(selected.id);
            DisplayCurrentBook();
            UpdateDetailNavigationButtons();
            return;
        }

        NavigateToFirstVisibleBook();
        if (BooksDataGrid.SelectedItem is null)
        {
            _currentBookIndex = -1;
            ClearDetails();
        }

        UpdateDetailNavigationButtons();
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

    private void DetailFirstButton_ClickHandler(object sender, RoutedEventArgs e) =>
        NavigateToFirstVisibleBook();

    private void DetailPreviousButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        var recordIndex = GetCurrentRecordIndex();
        if (recordIndex <= 0)
            return;

        var book = GetBookAtVisibleRecordIndex(recordIndex - 1);
        if (book is not null)
            NavigateToVisibleBook(book);
    }

    private void DetailNextButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        var recordIndex = GetCurrentRecordIndex();
        var recordCount = GetVisibleRecordCount();
        if (recordIndex < 0 || recordIndex >= recordCount - 1)
            return;

        var book = GetBookAtVisibleRecordIndex(recordIndex + 1);
        if (book is not null)
            NavigateToVisibleBook(book);
    }

    private void DetailLastButton_ClickHandler(object sender, RoutedEventArgs e) =>
        NavigateToLastVisibleBook();

    private void NavigateToBookId(long? bookId)
    {
        if (_allBooksCollection is null || _allBooksCollection.Count == 0 || bookId is null)
            return;

        var index = FindBookIndexById(bookId.Value);
        if (index < 0)
            return;

        NavigateToVisibleBook(_allBooksCollection[index]);
    }

    private void NavigateToFirstVisibleBook()
    {
        var book = GetBookAtVisibleRecordIndex(0);
        if (book is not null)
            NavigateToVisibleBook(book);
    }

    private void NavigateToLastVisibleBook()
    {
        var recordCount = GetVisibleRecordCount();
        if (recordCount == 0)
            return;

        var book = GetBookAtVisibleRecordIndex(recordCount - 1);
        if (book is not null)
            NavigateToVisibleBook(book);
    }

    private void NavigateToVisibleBook(book_ex book)
    {
        var index = FindBookIndexById(book.id);
        if (index < 0)
            return;

        _currentBookIndex = index;

        var rowIndex = BooksDataGrid.ResolveToRowIndex(book);
        if (rowIndex >= 0)
        {
            BooksDataGrid.SelectedItem = book;
            ScrollBookIntoView(book);
        }

        DisplayCurrentBook();
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
        DetailPriceText.Text = AppUtils.FormatPrice(summary.price);
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

        AppUtils.LoadCoverImage(full?.cover_image, CoverImage, NoImagePanel);
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
        AppUtils.LoadCoverImage(null, CoverImage, NoImagePanel);
    }

    private void UpdateDetailNavigationButtons()
    {
        var recordIndex = GetCurrentRecordIndex();
        var recordCount = GetVisibleRecordCount();

        if (recordIndex < 0 || recordCount == 0)
        {
            DetailFirstButton.IsEnabled = false;
            DetailPreviousButton.IsEnabled = false;
            DetailNextButton.IsEnabled = false;
            DetailLastButton.IsEnabled = false;
            return;
        }

        DetailFirstButton.IsEnabled = recordIndex > 0;
        DetailPreviousButton.IsEnabled = recordIndex > 0;
        DetailNextButton.IsEnabled = recordIndex < recordCount - 1;
        DetailLastButton.IsEnabled = recordIndex < recordCount - 1;
    }

    private void SyncGridSelectionToCurrentBook()
    {
        if (_allBooksCollection is null || _currentBookIndex < 0 || _currentBookIndex >= _allBooksCollection.Count)
            return;

        NavigateToVisibleBook(_allBooksCollection[_currentBookIndex]);
    }

    private book_ex? GetCurrentBook()
    {
        if (BooksDataGrid.SelectedItem is book_ex selected)
            return selected;

        if (_allBooksCollection is not null &&
            _currentBookIndex >= 0 &&
            _currentBookIndex < _allBooksCollection.Count)
            return _allBooksCollection[_currentBookIndex];

        return null;
    }

    private int GetCurrentRecordIndex()
    {
        var book = GetCurrentBook();
        if (book is null)
            return -1;

        var rowIndex = BooksDataGrid.ResolveToRowIndex(book);
        if (rowIndex < 0)
            return -1;

        return BooksDataGrid.ResolveToRecordIndex(rowIndex);
    }

    private int GetVisibleRecordCount() =>
        SelectionHelper.GetRecordsCount(BooksDataGrid, checkUnBoundRows: false);

    private book_ex? GetBookAtVisibleRecordIndex(int recordIndex)
    {
        if (recordIndex < 0 || recordIndex >= GetVisibleRecordCount())
            return null;

        var rowIndex = BooksDataGrid.ResolveToRowIndex(recordIndex);
        if (rowIndex < 0)
            return null;

        return SelectionHelper.GetRecordAtRowIndex(BooksDataGrid, rowIndex) as book_ex;
    }

    private void ScrollBookIntoView(book_ex book)
    {
        BooksDataGrid.Dispatcher.BeginInvoke(() =>
        {
            var rowIndex = BooksDataGrid.ResolveToRowIndex(book);
            if (rowIndex >= 0)
                BooksDataGrid.ScrollInView(new RowColumnIndex(rowIndex, 0));
        }, DispatcherPriority.Loaded);
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

        if (_publisherRepository is not null)
            TotalPublisherCountText.Text = string.Format("Total Publisher Count {0}", _publisherRepository.Count());

        if (_languageRepository is not null)
            TotalLanguageCountText.Text = string.Format("Total Language Count {0}", _languageRepository.Count());

        if (_cityRepository is not null)
            TotalCityCountText.Text = string.Format("Total City Count {0}", _cityRepository.Count());
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
    private ToolTip? _searchNothingFoundToolTip;
}
