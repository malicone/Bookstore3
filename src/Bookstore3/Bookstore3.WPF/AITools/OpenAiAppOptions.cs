using Bookstore3.Repository;
using Bookstore3.WPF.Utils;

namespace Bookstore3.WPF.AITools;

internal static class OpenAiAppOptionKeys
{
    public const string ApiKey = "Options.OpenAi.ApiKey";
    public const string Model = "Options.OpenAi.Model";
    public const string Endpoint = "Options.OpenAi.Endpoint";
    public const string DefaultModel = "gpt-4o-mini";
    public const string DefaultEndpoint = "https://api.openai.com/v1/chat/completions";
}

internal sealed class OpenAiAppOptions
{
    public string? ApiKey { get; init; }
    public string Model { get; init; } = OpenAiAppOptionKeys.DefaultModel;
    public string Endpoint { get; init; } = OpenAiAppOptionKeys.DefaultEndpoint;

    public static OpenAiAppOptions Load(IAppOptionRepository repository, IStringCipher cipher)
    {
        var encryptedKey = repository.GetOptionAsString(OpenAiAppOptionKeys.ApiKey);
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

        var model = repository.GetOptionAsString(OpenAiAppOptionKeys.Model);
        var endpoint = repository.GetOptionAsString(OpenAiAppOptionKeys.Endpoint);

        return new OpenAiAppOptions
        {
            ApiKey = apiKey,
            Model = string.IsNullOrWhiteSpace(model) ? OpenAiAppOptionKeys.DefaultModel : model,
            Endpoint = string.IsNullOrWhiteSpace(endpoint) ? OpenAiAppOptionKeys.DefaultEndpoint : endpoint
        };
    }

    public static bool Save(IAppOptionRepository repository, IStringCipher cipher, OpenAiAppOptions options)
    {
        var encryptedKey = string.IsNullOrEmpty(options.ApiKey)
            ? string.Empty
            : cipher.Encrypt(options.ApiKey);

        var result = repository.SetOptionAsString(OpenAiAppOptionKeys.ApiKey, encryptedKey);
        if (repository.SetOptionAsString(OpenAiAppOptionKeys.Model, options.Model) == false)
            result = false;
        if (repository.SetOptionAsString(OpenAiAppOptionKeys.Endpoint, options.Endpoint) == false)
            result = false;
        return result;
    }
}
