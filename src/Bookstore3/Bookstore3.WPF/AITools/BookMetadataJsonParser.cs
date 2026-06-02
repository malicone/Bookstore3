using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bookstore3.WPF.AITools;

internal static class BookMetadataJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters =
        {
            new LenientNullableInt32JsonConverter(),
            new LenientNullableDoubleJsonConverter()
        }
    };

    public static BookMetadataResult Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("AI returned empty content.");

        var json = ExtractJsonObject(content);
        try
        {
            var metadata = JsonSerializer.Deserialize<BookMetadataResult>(json, JsonOptions);
            if (metadata is null)
                throw new InvalidOperationException("AI response is not valid JSON metadata.");

            return metadata;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"AI metadata JSON is invalid or uses unsupported field types: {ex.Message}", ex);
        }
    }

    private static string ExtractJsonObject(string content)
    {
        content = content.Trim();
        if (content.StartsWith("```", StringComparison.Ordinal))
        {
            var lineBreak = content.IndexOf('\n');
            if (lineBreak >= 0)
                content = content[(lineBreak + 1)..];

            var fenceEnd = content.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceEnd > 0)
                content = content[..fenceEnd];
        }

        content = content.Trim();
        var firstBrace = content.IndexOf('{');
        var lastBrace = content.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
            content = content[firstBrace..(lastBrace + 1)];

        return content.Trim();
    }
}
