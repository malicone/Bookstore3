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

    public static IReadOnlyList<BookMetadataResult> ParseList(
        string content,
        IAiDebugLog debugLog,
        string? debugContext = null)
    {
        var contextPrefix = string.IsNullOrWhiteSpace(debugContext) ? "ParseList" : $"ParseList ({debugContext})";

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("AI returned empty content.");

        var json = ExtractJsonPayload(content);
        debugLog.Write($"{contextPrefix}: extracted JSON length={json.Length}");
        debugLog.WriteResponseSnippet($"{contextPrefix} JSON", json, maxLength: 600);

        try
        {
            var books = TryDeserializeBooksArray(json);
            if (books is null)
                throw new InvalidOperationException("AI response is not valid JSON metadata.");

            debugLog.Write($"{contextPrefix}: deserialized raw book count={books.Count}");

            var filtered = books.Where(HasIdentifyingMetadata).ToList();
            debugLog.Write($"{contextPrefix}: after filter count={filtered.Count}");

            if (filtered.Count == 0 && books.Count > 0)
            {
                debugLog.Write($"{contextPrefix}: filter removed all books, returning unfiltered list.");
                BookMetadataCoverEnricher.EnrichCoverImageUrls(books);
                return books;
            }

            BookMetadataCoverEnricher.EnrichCoverImageUrls(filtered);
            return filtered;
        }
        catch (JsonException ex)
        {
            debugLog.WriteException(contextPrefix, ex);
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

        var booksObjectStart = FindBooksWrapperStart(content);
        if (booksObjectStart >= 0)
        {
            var lastObject = content.LastIndexOf('}');
            if (lastObject > booksObjectStart)
                return content[booksObjectStart..(lastObject + 1)].Trim();
        }

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

    private static int FindBooksWrapperStart(string content)
    {
        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        var markers = new[] { "{\"books\"", "{ \"books\"", "{\n\"books\"", "{\r\n\"books\"" };
        var start = -1;
        foreach (var marker in markers)
        {
            var index = content.IndexOf(marker, comparison);
            if (index >= 0 && (start < 0 || index < start))
                start = index;
        }

        return start;
    }

    private static bool HasIdentifyingMetadata(BookMetadataResult book) =>
        string.IsNullOrWhiteSpace(book.title) == false ||
        string.IsNullOrWhiteSpace(book.author) == false ||
        string.IsNullOrWhiteSpace(book.isbn) == false ||
        string.IsNullOrWhiteSpace(book.publisher) == false ||
        book.publishYear.HasValue ||
        string.IsNullOrWhiteSpace(book.coverImageUrl) == false;
}