using Bookstore3.Model;
using Bookstore3.Repository;
using KpzRepository.Repository;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Bookstore3.WPF;

public partial class BookWindow : Window, IOptionsSavable
{
    public long SavedBookId { get; private set; } = AppConstants.NullRecordId;

    public BookWindow(IBookstoreRepositoryFactory repositoryFactory, long bookId = AppConstants.NullRecordId)
    {
        InitializeComponent();

        _repositoryFactory = repositoryFactory;
        _bookId = bookId;
        _appOptionRepository = repositoryFactory.GetAppOptionRepository();

        _bookRepository = repositoryFactory.GetBookRepository();
        _groupRepository = repositoryFactory.GetBaseRepository<long, group>();
        _publisherRepository = repositoryFactory.GetBaseRepository<long, publisher>();
        _shopRepository = repositoryFactory.GetBaseRepository<long, shop>();
        _languageRepository = repositoryFactory.GetBaseRepository<long, language>();
        _cityRepository = repositoryFactory.GetBaseRepository<long, city>();

        try
        {
            ApplyWindowOptionsFromDatabase();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"An error occurred while loading window options: {ex.Message}");
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
        GroupComboBox.ItemsSource = _groupRepository.GetAll().ToList();
        PublisherComboBox.ItemsSource = _publisherRepository.GetAll().ToList();
        ShopComboBox.ItemsSource = _shopRepository.GetAll().ToList();
        LanguageComboBox.ItemsSource = _languageRepository.GetAll().ToList();
        CityComboBox.ItemsSource = _cityRepository.GetAll().ToList();
    }

    private void InitLookupComboBoxesToUndefined()
    {
        GroupComboBox.SelectedValue = AppConstants.UndefinedRecordId;
        PublisherComboBox.SelectedValue = AppConstants.UndefinedRecordId;
        ShopComboBox.SelectedValue = AppConstants.UndefinedRecordId;
        LanguageComboBox.SelectedValue = AppConstants.UndefinedRecordId;
        CityComboBox.SelectedValue = AppConstants.UndefinedRecordId;
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

    private byte[]? _coverImageBytes;

    private const string _OptionsPrefix = "BookWindow";
    private const string _SelectedTabIndexOptionName = "SelectedTabIndex";
}
