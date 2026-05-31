using Bookstore3.Model;
using Bookstore3.Repository;
using KpzRepository.Repository;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace Bookstore3.WPF;

public partial class BookWindow : Window
{
    public BookWindow(IBookstoreRepositoryFactory repositoryFactory, long bookId = AppConstants.NullRecordId)
    {
        InitializeComponent();

        _repositoryFactory = repositoryFactory;
        _bookId = bookId;

        _bookRepository = repositoryFactory.GetBookRepository();
        _groupRepository = repositoryFactory.GetBaseRepository<long, group>();
        _publisherRepository = repositoryFactory.GetBaseRepository<long, publisher>();
        _shopRepository = repositoryFactory.GetBaseRepository<long, shop>();
        _languageRepository = repositoryFactory.GetBaseRepository<long, language>();
        _cityRepository = repositoryFactory.GetBaseRepository<long, city>();
    }

    private void BookWindow_LoadedHandler(object sender, RoutedEventArgs e)
    {
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

    private readonly IBookstoreRepositoryFactory _repositoryFactory;
    private readonly long _bookId;

    private readonly IBookRepository _bookRepository;
    private readonly IKpzRepository<long, group> _groupRepository;
    private readonly IKpzRepository<long, publisher> _publisherRepository;
    private readonly IKpzRepository<long, shop> _shopRepository;
    private readonly IKpzRepository<long, language> _languageRepository;
    private readonly IKpzRepository<long, city> _cityRepository;

    private byte[]? _coverImageBytes;
}
