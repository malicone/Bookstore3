using Bookstore3.Repository;
using Bookstore3.WPF.Utils;

namespace Bookstore3.WPF.AITools;

internal static class GoogleAppOptionKeys
{
    public const string ApiKey = "Options.Google.ApiKey";
    public const string Model = "Options.Google.Model";
    public const string DefaultModel = "gemini-2.5-flash";
}

internal sealed class GoogleAppOptions
{
    public string? ApiKey { get; init; }
    public string Model { get; init; } = GoogleAppOptionKeys.DefaultModel;

    public static GoogleAppOptions Load(IAppOptionRepository repository, IStringCipher cipher)
    {
        var encryptedKey = repository.GetOptionAsString(GoogleAppOptionKeys.ApiKey);
        string? apiKey = null;
        if (string.IsNullOrEmpty(encryptedKey) == false)
        {
            try
            {
                apiKey = cipher.Decrypt(encryptedKey);
            }
            catch
            {
                apiKey = null;
            }
        }

        var model = repository.GetOptionAsString(GoogleAppOptionKeys.Model);

        return new GoogleAppOptions
        {
            ApiKey = apiKey,
            Model = string.IsNullOrWhiteSpace(model) ? GoogleAppOptionKeys.DefaultModel : model
        };
    }

    public static bool Save(IAppOptionRepository repository, IStringCipher cipher, GoogleAppOptions options)
    {
        var encryptedKey = string.IsNullOrEmpty(options.ApiKey)
            ? string.Empty
            : cipher.Encrypt(options.ApiKey);

        var result = repository.SetOptionAsString(GoogleAppOptionKeys.ApiKey, encryptedKey);
        if (repository.SetOptionAsString(GoogleAppOptionKeys.Model, options.Model) == false)
            result = false;
        return result;
    }
}
