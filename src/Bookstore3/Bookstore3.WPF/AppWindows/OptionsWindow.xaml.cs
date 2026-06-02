using Bookstore3.Repository;
using Bookstore3.WPF.AITools;
using Bookstore3.WPF.Options;
using Bookstore3.WPF.Utils;
using System.ComponentModel;
using System.Windows;

namespace Bookstore3.WPF.AppWindows;

public partial class OptionsWindow : Window, IOptionsSavable
{
    public OptionsWindow(IAppOptionRepository appOptionRepository, IStringCipher? stringCipher = null)
    {
        InitializeComponent();

        _appOptionRepository = appOptionRepository;
        _stringCipher = stringCipher ?? new AesStringCipher();

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
        return WindowOptionsPersistence.Save(_appOptionRepository, this, GetFullOptionName);
    }

    public bool LoadOptions() => ApplyWindowOptionsFromDatabase();

    private void OptionsWindow_LoadedHandler(object sender, RoutedEventArgs e)
    {
        LoadOpenAiOptionsToUi();
        LoadGoogleOptionsToUi();
    }

    private void OptionsWindow_ClosingHandler(object? sender, CancelEventArgs e)
    {
        if (_savedProviderOptions)
            return;

        try
        {
            SaveOptions();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Failed to save options: {ex.Message}");
        }
    }

    private void OkButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        try
        {
            if (SaveOpenAiOptionsFromUi() == false)
            {
                AppUtils.ShowErrorMessage("Failed to save Open AI options.");
                return;
            }

            if (SaveGoogleOptionsFromUi() == false)
            {
                AppUtils.ShowErrorMessage("Failed to save Google options.");
                return;
            }

            if (SaveOptions() == false)
                AppUtils.ShowErrorMessage("Failed to save window options.");

            _savedProviderOptions = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Failed to save options: {ex.Message}");
        }
    }

    private void CancelButton_ClickHandler(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void LoadOpenAiOptionsToUi()
    {
        var options = OpenAiAppOptions.Load(_appOptionRepository, _stringCipher);
        OpenAiApiKeyTextBox.Text = options.ApiKey ?? string.Empty;
        OpenAiModelTextBox.Text = options.Model;
        OpenAiEndpointTextBox.Text = options.Endpoint;
    }

    private bool SaveOpenAiOptionsFromUi()
    {
        var options = new OpenAiAppOptions
        {
            ApiKey = OpenAiApiKeyTextBox.Text?.Trim(),
            Model = string.IsNullOrWhiteSpace(OpenAiModelTextBox.Text)
                ? OpenAiAppOptionKeys.DefaultModel
                : OpenAiModelTextBox.Text.Trim(),
            Endpoint = string.IsNullOrWhiteSpace(OpenAiEndpointTextBox.Text)
                ? OpenAiAppOptionKeys.DefaultEndpoint
                : OpenAiEndpointTextBox.Text.Trim()
        };

        return OpenAiAppOptions.Save(_appOptionRepository, _stringCipher, options);
    }

    private void LoadGoogleOptionsToUi()
    {
        var options = GoogleAppOptions.Load(_appOptionRepository, _stringCipher);
        GoogleApiKeyTextBox.Text = options.ApiKey ?? string.Empty;
        GoogleModelTextBox.Text = options.Model;
    }

    private bool SaveGoogleOptionsFromUi()
    {
        var options = new GoogleAppOptions
        {
            ApiKey = GoogleApiKeyTextBox.Text?.Trim(),
            Model = string.IsNullOrWhiteSpace(GoogleModelTextBox.Text)
                ? GoogleAppOptionKeys.DefaultModel
                : GoogleModelTextBox.Text.Trim()
        };

        return GoogleAppOptions.Save(_appOptionRepository, _stringCipher, options);
    }

    private string GetFullOptionName(string optionName) => $"{_OptionsPrefix}.{optionName}";

    private bool ApplyWindowOptionsFromDatabase()
    {
        return WindowOptionsPersistence.TryApply(
            _appOptionRepository,
            this,
            GetFullOptionName,
            MinWidth,
            MinHeight);
    }

    private readonly IAppOptionRepository _appOptionRepository;
    private readonly IStringCipher _stringCipher;
    private bool _savedProviderOptions;

    private const string _OptionsPrefix = "OptionsWindow";
}
