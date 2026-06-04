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

    public static IReadOnlyList<BookMetadataResult> ParseList(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("AI returned empty content.");

        var json = ExtractJsonPayload(content);
        try
        {
            var books = TryDeserializeBooksArray(json);
            if (books is null)
                throw new InvalidOperationException("AI response is not valid JSON metadata.");

            return books
                .Where(book => string.IsNullOrWhiteSpace(book.title) == false ||
                               string.IsNullOrWhiteSpace(book.author) == false ||
                               string.IsNullOrWhiteSpace(book.isbn) == false)
                .ToList();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"AI metadata JSON is invalid or uses unsupported field types: {ex.Message}", ex);
        }
    }

    private static List<BookMetadataResult>? TryDeserializeBooksArray(string json)
    {
        if (json.StartsWith("[", StringComparison.Ordinal))
            return JsonSerializer.Deserialize<List<BookMetadataResult>>(json, JsonOptions);

        var wrapper = JsonSerializer.Deserialize<BookMetadataListResponse>(json, JsonOptions);
        return wrapper?.books;
    }

    private static string ExtractJsonPayload(string content)
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
        var firstArray = content.IndexOf('[');
        var firstObject = content.IndexOf('{');
        if (firstArray >= 0 && (firstObject < 0 || firstArray < firstObject))
        {
            var lastArray = content.LastIndexOf(']');
            if (lastArray > firstArray)
                return content[firstArray..(lastArray + 1)].Trim();
        }

        if (firstObject >= 0)
        {
            var lastObject = content.LastIndexOf('}');
            if (lastObject > firstObject)
                return content[firstObject..(lastObject + 1)].Trim();
        }

        return content;
    }
}