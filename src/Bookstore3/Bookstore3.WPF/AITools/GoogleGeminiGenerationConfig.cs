using Mscc.GenerativeAI.Types;

namespace Bookstore3.WPF.AITools;

internal static class GoogleGeminiGenerationConfig
{
    public static GenerationConfig ForMetadataFetch(string modelName, bool jsonResponse)
    {
        var config = new GenerationConfig
        {
            Temperature = 0.2f,
            MaxOutputTokens = 8192
        };

        if (jsonResponse)
            config.ResponseMimeType = "application/json";

        if (IsGemini3OrLater(modelName))
        {
            config.ThinkingConfig = new ThinkingConfig
            {
                ThinkingLevel = ThinkingLevel.Minimal,
                IncludeThoughts = false
            };
        }

        return config;
    }

    public static bool IsGemini3OrLater(string modelName) =>
        modelName.Contains("gemini-3", StringComparison.OrdinalIgnoreCase);
}
