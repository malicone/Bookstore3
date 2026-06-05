using Bookstore3.Repository;
using Bookstore3.WPF.AITools;
using Bookstore3.WPF.Options;
using Bookstore3.WPF.Utils;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

namespace Bookstore3.WPF.AppWindows;

public partial class BookMetadataPickerDialog : Window, IOptionsSavable
{
    public BookMetadataPickerDialog(
        IReadOnlyList<BookMetadataResult> results,
        IAppOptionRepository? appOptionRepository = null)
    {
        _appOptionRepository = appOptionRepository;

        InitializeComponent();
        _items = results.Select(result => new BookMetadataPickerItem(result)).ToList();
        ResultsListView.ItemsSource = _items;

        try
        {
            LoadOptions();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"An error occurred while loading window options: {ex.Message}");
        }
    }

    public BookMetadataResult? SelectedMetadata { get; private set; }

    public bool SaveOptions()
    {
        if (_appOptionRepository is null)
            return false;

        return WindowOptionsPersistence.Save(_appOptionRepository, this, GetFullOptionName);
    }

    public bool LoadOptions() => ApplyWindowOptionsFromDatabase();

    private async void Window_LoadedHandler(object sender, RoutedEventArgs e)
    {
        if (ResultsListView.Items.Count > 0)
            ResultsListView.SelectedIndex = 0;

        ResultsListView.Focus();

        _loadCoversCts = new CancellationTokenSource();
        try
        {
            await Task.WhenAll(_items.Select(item =>
                item.LoadCoverImageAsync(_httpClient, _loadCoversCts.Token)));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Window_ClosingHandler(object? sender, CancelEventArgs e)
    {
        try
        {
            SaveOptions();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Failed to save options: {ex.Message}");
        }

        _loadCoversCts?.Cancel();
        _loadCoversCts?.Dispose();
        _httpClient.Dispose();
    }

    private void OkButton_ClickHandler(object sender, RoutedEventArgs e) => ConfirmSelection();

    private void ResultsListView_MouseDoubleClickHandler(object sender, MouseButtonEventArgs e) =>
        ConfirmSelection();

    private void ConfirmSelection()
    {
        if (ResultsListView.SelectedItem is not BookMetadataPickerItem selected)
        {
            AppUtils.ShowInfoMessage("Please select a book.");
            return;
        }

        SelectedMetadata = selected.Metadata;
        DialogResult = true;
    }

    private string GetFullOptionName(string optionName) => $"{_OptionsPrefix}.{optionName}";

    private bool ApplyWindowOptionsFromDatabase()
    {
        if (_appOptionRepository is null)
            return false;

        return WindowOptionsPersistence.TryApply(
            _appOptionRepository,
            this,
            GetFullOptionName,
            MinWidth,
            MinHeight);
    }

    private readonly IAppOptionRepository? _appOptionRepository;
    private readonly List<BookMetadataPickerItem> _items;
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private CancellationTokenSource? _loadCoversCts;

    private const string _OptionsPrefix = "BookMetadataPickerDialog";
}
