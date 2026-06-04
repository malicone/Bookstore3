using Bookstore3.Model;
using Bookstore3.Model.Abstract;
using Bookstore3.Repository;
using Bookstore3.WPF.AITools;
using Bookstore3.WPF.Options;
using Bookstore3.WPF.Utils;
using KpzRepository.Repository;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Bookstore3.WPF.AppWindows;

public partial class BookWindow : Window, IOptionsSavable
{
    public long SavedBookId { get; private set; } = AppConstants.NullRecordId;

    public BookWindow(IBookstoreRepositoryFactory repositoryFactory, long bookId = AppConstants.NullRecordId)
    {
        InitializeComponent();

        _repositoryFactory = repositoryFactory;
        _stringCipher = new AesStringCipher();
        _bookId = bookId;
        _appOptionRepository = repositoryFactory.GetAppOptionRepository();

        _bookRepository = repositoryFactory.GetBookRepository();
        _groupRepository = repositoryFactory.GetBaseRepository<long, group>();
        _publisherRepository = repositoryFactory.GetBaseRepository<long, publisher>();
        _shopRepository = repositoryFactory.GetBaseRepository<long, shop>();
        _languageRepository = repositoryFactory.GetBaseRepository<long, language>();
        _cityRepository = repositoryFactory.GetBaseRepository<long, city>();

        if (_appOptionRepository is not null)
        {
            _openAiBookMetadataService = new OpenAiBookMetadataService(_appOptionRepository, _stringCipher);
            _googleAiBookMetadataService = new GoogleAiBookMetadataService(_appOptionRepository, _stringCipher);
        }

        try
        {
            ApplyWindowOptionsFromDatabase();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"An error occurred while loading window options: {ex.Message}");
        }
    }

    private async void FetchFromOpenAiButton_ClickHandler(object sender, RoutedEventArgs e) =>
        await FetchMetadataAsync(_openAiBookMetadataService, FetchFromOpenAiButton, "Open AI");

    private async void FetchFromGoogleAiButton_ClickHandler(object sender, RoutedEventArgs e) =>
        await FetchMetadataAsync(_googleAiBookMetadataService, FetchFromGoogleAiButton, "Google AI");

    private async Task FetchMetadataAsync(
        IAiBookMetadataService? metadataService,
        Button fetchButton,
        string providerName)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
        {
            AppUtils.ShowInfoMessage("Please enter Title.");
            SwitchToMainDataTab();
            TitleTextBox.Focus();
            return;
        }

        if (metadataService is null)
        {
            AppUtils.ShowErrorMessage("Application options are not available.");
            return;
        }

        fetchButton.IsEnabled = false;
        var originalToolTip = fetchButton.ToolTip;
        fetchButton.ToolTip = $"Fetching metadata from {providerName}...";

        try
        {
            var author = string.IsNullOrWhiteSpace(AuthorTextBox.Text) ? null : AuthorTextBox.Text.Trim();
            int? edition = EditionIntegerTextBox.Value is long editionValue
                ? (int)editionValue
                : null;
            var results = await metadataService.FetchMetadataAsync(
                TitleTextBox.Text.Trim(),
                author,
                edition,
                CancellationToken.None);

            if (results.Count == 0)
            {
                AppUtils.ShowInfoMessage($"No matching books were found from {providerName}.");
                return;
            }

            var picker = new BookMetadataPickerDialog(results)
            {
                Owner = this
            };
            if (picker.ShowDialog() != true || picker.SelectedMetadata is not BookMetadataResult selectedMetadata)
                return;

            ApplyFetchedMetadata(selectedMetadata);
            await DownloadAndApplyCoverImageAsync(selectedMetadata.coverImageUrl);
            AppUtils.ShowInfoMessage($"Book metadata fetched from {providerName}.");
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Failed to fetch metadata: {ex.Message}");
        }
        finally
        {
            fetchButton.IsEnabled = true;
            fetchButton.ToolTip = originalToolTip;
        }
    }

    public bool SaveOptions()
    {
        if (_appOptionRepository is null)
            return false;

        var result = WindowOptionsPersistence.Save(_appOptionRepository, this, GetFullOptionName);
        if (_appOptionRepository.SetOptionAsLong(
                GetFullOptionName(_SelectedTabIndexOptionName),
                BookTabControl.SelectedIndex) == false)
            result = false;

        return result;
    }

    public bool LoadOptions()
    {
        var windowLoaded = ApplyWindowOptionsFromDatabase();
        var tabLoaded = ApplySelectedTabFromDatabase();
        return windowLoaded || tabLoaded;
    }

    private void BookWindow_ClosingHandler(object? sender, CancelEventArgs e)
    {
        try
        {
            SaveOptions();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Failed to save options: {ex.Message}");
        }
    }

    private void BookWindow_LoadedHandler(object sender, RoutedEventArgs e)
    {
        ApplySelectedTabFromDatabase();

        if (_bookId == AppConstants.NullRecordId)
        {
            Title = "Add New Book";
        }
        else
        {
            Title = "Edit Book";
        }

        PopulateLookupComboBoxes();

        if (_bookId == AppConstants.NullRecordId)
        {
            EditionIntegerTextBox.Value = 1;
            var now = DateTime.Now;
            PublishYearDatePicker.Value = new DateTime(now.Year, 1, 1);
            GotAtDateTimeEdit.DateTime = now;
            InitLookupComboBoxesToUndefined();
            return;
        }

        var book = _bookRepository.Get(_bookId);
        if (book is null)
            return;

        _coverImageBytes = AppUtils.LoadCoverImage(book.cover_image, CoverImage, NoImagePanel);
        LoadMainDataFromBook(book);
    }

    private void PopulateLookupComboBoxes()
    {
        RefreshGroupComboBox();
        RefreshPublisherComboBox();
        RefreshShopComboBox();
        RefreshLanguageComboBox();
        RefreshCityComboBox();
    }

    private void InitLookupComboBoxesToUndefined()
    {
        GroupComboBox.SelectedValue = AppConstants.UndefinedRecordId;
        PublisherComboBox.SelectedValue = AppConstants.UndefinedRecordId;
        ShopComboBox.SelectedValue = AppConstants.UndefinedRecordId;
        LanguageComboBox.SelectedValue = AppConstants.UndefinedRecordId;
        CityComboBox.SelectedValue = AppConstants.UndefinedRecordId;
    }

    private void ApplyFetchedMetadata(BookMetadataResult metadata)
    {
        TitleTextBox.Text = metadata.title ?? string.Empty;
        AuthorTextBox.Text = metadata.author ?? string.Empty;
        IsbnTextBox.Text = metadata.isbn ?? string.Empty;
        PageCountIntegerTextBox.Value = metadata.pageCount;
        EditionIntegerTextBox.Value = metadata.edition;
        FormatTextBox.Text = metadata.format ?? string.Empty;
        PublishYearDatePicker.Value = metadata.publishYear.HasValue
            ? new DateTime(metadata.publishYear.Value, 1, 1)
            : null;
        PriceDoubleTextBox.Value = metadata.price;
        AnnotationTextBox.Text = metadata.annotation ?? string.Empty;

        TrySelectLookupByName(GroupComboBox, metadata.@group);
        TrySelectLookupByName(PublisherComboBox, metadata.publisher);
        TrySelectLookupByName(LanguageComboBox, metadata.language);
        TrySelectLookupByName(CityComboBox, metadata.city);
    }

    private static void TrySelectLookupByName(Selector selector, string? lookupName)
    {
        if (string.IsNullOrWhiteSpace(lookupName) || selector.ItemsSource is null)
            return;

        var normalizedTarget = NormalizeLookup(lookupName);
        foreach (var item in selector.ItemsSource)
        {
            if (item is not lookup_entity lookup || string.IsNullOrWhiteSpace(lookup.name))
                continue;

            if (NormalizeLookup(lookup.name) == normalizedTarget)
            {
                selector.SelectedValue = lookup.id;
                return;
            }
        }
    }

    private static string NormalizeLookup(string value) =>
        value.Trim().ToLowerInvariant();

    private async Task DownloadAndApplyCoverImageAsync(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) == false ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return;

        try
        {
            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            const int maxImageBytes = 5 * 1024 * 1024;
            if (memoryStream.Length > maxImageBytes)
                throw new InvalidOperationException("Cover image is too large.");

            _coverImageBytes = AppUtils.LoadCoverImage(memoryStream.ToArray(), CoverImage, NoImagePanel);
        }
        catch (Exception ex)
        {
            AppUtils.ShowInfoMessage($"Cover image was not loaded: {ex.Message}");
        }
    }

    private void LoadMainDataFromBook(book book)
    {
        AuthorTextBox.Text = book.author ?? string.Empty;
        TitleTextBox.Text = book.title;
        GroupComboBox.SelectedValue = book.group_id;
        IsbnTextBox.Text = book.isbn ?? string.Empty;
        EditionIntegerTextBox.Value = book.edition;
        PageCountIntegerTextBox.Value = book.page_count;
        PublisherComboBox.SelectedValue = book.publisher_id;
        ShopComboBox.SelectedValue = book.shop_id;
        FormatTextBox.Text = book.format ?? string.Empty;
        PublishYearDatePicker.Value = book.publish_year.HasValue
            ? new DateTime(book.publish_year.Value, 1, 1)
            : null;
        PriceDoubleTextBox.Value = book.price;
        GotAtDateTimeEdit.DateTime = book.date_when_get;
        HardcoverCheckBox.IsChecked = book.wrapper;
        LanguageComboBox.SelectedValue = book.language_id;
        DigitalCopyCheckBox.IsChecked = book.has_digit_copy;
        CityComboBox.SelectedValue = book.city_id;
        BookFileTextBox.Text = book.book_file ?? string.Empty;
        AnnotationTextBox.Text = book.annotation ?? string.Empty;
        DetailsTextBox.Text = book.details ?? string.Empty;
    }

    private void ManageGroupsButton_ClickHandler(object sender, RoutedEventArgs e) =>
        ManageLookup<group>(GroupComboBox, RefreshGroupComboBox, "Groups");

    private void ManagePublishersButton_ClickHandler(object sender, RoutedEventArgs e) =>
        ManageLookup<publisher>(PublisherComboBox, RefreshPublisherComboBox, "Publishers");

    private void ManageShopsButton_ClickHandler(object sender, RoutedEventArgs e) =>
        ManageLookup<shop>(ShopComboBox, RefreshShopComboBox, "Shops");

    private void ManageLanguagesButton_ClickHandler(object sender, RoutedEventArgs e) =>
        ManageLookup<language>(LanguageComboBox, RefreshLanguageComboBox, "Languages");

    private void ManageCitiesButton_ClickHandler(object sender, RoutedEventArgs e) =>
        ManageLookup<city>(CityComboBox, RefreshCityComboBox, "Cities");

    private void ManageLookup<TLookup>(
        Selector comboBox,
        Action refreshComboBox,
        string windowTitle) where TLookup : lookup_entity, new()
    {
        var previousSelection = comboBox.SelectedValue;
        var lookupWindow = new BaseLookupWindow<TLookup>(_repositoryFactory, windowTitle)
        {
            Owner = this
        };
        lookupWindow.ShowDialog();

        refreshComboBox();

        if (lookupWindow.LastCreatedRecordId is long newRecordId and > 0)
            comboBox.SelectedValue = newRecordId;
        else if (previousSelection is not null)
            comboBox.SelectedValue = previousSelection;
    }

    private void RefreshGroupComboBox() =>
        GroupComboBox.ItemsSource = _groupRepository.GetAll().ToList();

    private void RefreshPublisherComboBox() =>
        PublisherComboBox.ItemsSource = _publisherRepository.GetAll().ToList();

    private void RefreshShopComboBox() =>
        ShopComboBox.ItemsSource = _shopRepository.GetAll().ToList();

    private void RefreshLanguageComboBox() =>
        LanguageComboBox.ItemsSource = _languageRepository.GetAll().ToList();

    private void RefreshCityComboBox() =>
        CityComboBox.ItemsSource = _cityRepository.GetAll().ToList();

    private void ChooseBookFileButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose Book File",
            Filter = "All Files|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        BookFileTextBox.Text = dialog.FileName;
    }

    private void OkButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        if (!ValidateTitle())
            return;

        try
        {
            if (_bookId == AppConstants.NullRecordId)
            {
                var book = CreateBookFromForm();
                book.crt_date_time = DateTime.Now;
                _bookRepository.Add(book);
                SavedBookId = book.id > 0 ? book.id : _bookRepository.GetMaxId();
            }
            else
            {
                var book = _bookRepository.Get(_bookId);
                if (book is null)
                {
                    AppUtils.ShowErrorMessage("Book not found.");
                    return;
                }

                ApplyFormToBook(book);
                _bookRepository.Update(book);
                SavedBookId = _bookId;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Failed to save book: {ex.Message}");
        }
    }

    private bool ValidateTitle()
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text) == false)
            return true;

        AppUtils.ShowInfoMessage("Please enter Title.");
        SwitchToMainDataTab();
        TitleTextBox.Focus();
        TitleTextBox.SelectAll();
        return false;
    }

    private void SwitchToMainDataTab()
    {
        if (BookTabControl.SelectedItem != MainDataTabItem)
            BookTabControl.SelectedItem = MainDataTabItem;
    }

    private book CreateBookFromForm()
    {
        var book = new book();
        ApplyFormToBook(book);
        return book;
    }

    private void ApplyFormToBook(book book)
    {
        book.title = TitleTextBox.Text.Trim();
        book.author = NullIfWhiteSpace(AuthorTextBox.Text);
        book.group_id = GetSelectedLong(GroupComboBox.SelectedValue);
        book.isbn = NullIfWhiteSpace(IsbnTextBox.Text);
        book.edition = EditionIntegerTextBox.Value.HasValue
            ? (int)EditionIntegerTextBox.Value.Value
            : 1;
        book.page_count = PageCountIntegerTextBox.Value.HasValue
            ? (int)PageCountIntegerTextBox.Value.Value
            : null;
        book.publisher_id = GetSelectedLong(PublisherComboBox.SelectedValue);
        book.shop_id = GetSelectedLong(ShopComboBox.SelectedValue);
        book.format = NullIfWhiteSpace(FormatTextBox.Text);
        book.publish_year = PublishYearDatePicker.Value?.Year;
        book.price = PriceDoubleTextBox.Value;
        book.date_when_get = GotAtDateTimeEdit.DateTime;
        book.wrapper = HardcoverCheckBox.IsChecked == true;
        book.language_id = GetSelectedLong(LanguageComboBox.SelectedValue);
        book.has_digit_copy = DigitalCopyCheckBox.IsChecked == true;
        book.city_id = GetSelectedLong(CityComboBox.SelectedValue);
        book.book_file = NullIfWhiteSpace(BookFileTextBox.Text);
        book.annotation = NullIfWhiteSpace(AnnotationTextBox.Text);
        book.details = NullIfWhiteSpace(DetailsTextBox.Text);
        book.cover_image = _coverImageBytes;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static long GetSelectedLong(object? selectedValue) =>
        selectedValue switch
        {
            long id => id,
            int intId => intId,
            _ => AppConstants.UndefinedRecordId
        };

    private void CancelButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CoverImageArea_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var height = e.NewSize.Height;
        if (height <= 0)
            return;

        CoverImageBorder.Height = height;
        CoverImageBorder.Width = height * AppConstants.CoverImageWidthToHeightRatio;
    }

    private void AddCoverImageButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Cover Image",
            Filter = "Image Files|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.webp;*.jfif;*.heic;*.heif|All Files|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var imageBytes = File.ReadAllBytes(dialog.FileName);
            _coverImageBytes = AppUtils.LoadCoverImage(imageBytes, CoverImage, NoImagePanel);
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Failed to load image: {ex.Message}");
        }
    }

    private void ClearCoverImageButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        _coverImageBytes = AppUtils.LoadCoverImage(null, CoverImage, NoImagePanel);
    }

    private void RotateCoverLeftButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        RotateCoverImage(-90);
    }

    private void RotateCoverRightButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        RotateCoverImage(90);
    }

    private void RotateCoverImage(double angle)
    {
        if (_coverImageBytes is not { Length: > 0 })
            return;

        try
        {
            var rotatedBytes = AppUtils.RotateImageBytes(_coverImageBytes, angle);
            _coverImageBytes = AppUtils.LoadCoverImage(rotatedBytes, CoverImage, NoImagePanel);
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Failed to rotate image: {ex.Message}");
        }
    }

    private string GetFullOptionName(string optionName) => $"{_OptionsPrefix}.{optionName}";

    private bool ApplyWindowOptionsFromDatabase()
    {
        if(_appOptionRepository is null)
            return false;

        return WindowOptionsPersistence.TryApply(
            _appOptionRepository,
            this,
            GetFullOptionName,
            MinWidth,
            MinHeight);
    }

    private bool ApplySelectedTabFromDatabase()
    {
        if(_appOptionRepository is null)
            return false;

        var tabIndex = _appOptionRepository.GetOptionAsLong(GetFullOptionName(_SelectedTabIndexOptionName));
        if(tabIndex is null || tabIndex < 0 || tabIndex >= BookTabControl.Items.Count)
            return false;

        BookTabControl.SelectedIndex = (int)tabIndex.Value;
        return true;
    }

    private readonly IBookstoreRepositoryFactory _repositoryFactory;
    private readonly long _bookId;

    private readonly IBookRepository _bookRepository;
    private readonly IKpzRepository<long, group> _groupRepository;
    private readonly IKpzRepository<long, publisher> _publisherRepository;
    private readonly IKpzRepository<long, shop> _shopRepository;
    private readonly IKpzRepository<long, language> _languageRepository;
    private readonly IKpzRepository<long, city> _cityRepository;
    private readonly IAppOptionRepository? _appOptionRepository;
    private readonly IStringCipher _stringCipher;
    private readonly IAiBookMetadataService? _openAiBookMetadataService;
    private readonly IAiBookMetadataService? _googleAiBookMetadataService;
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private byte[]? _coverImageBytes;

    private const string _OptionsPrefix = "BookWindow";
    private const string _SelectedTabIndexOptionName = "SelectedTabIndex";
}