using System.Text.RegularExpressions;

namespace Bookstore3.WPF.AITools;

internal static class BookMetadataCoverEnricher
{
    private static readonly Regex IsbnDigitsOnly = new(@"[^0-9Xx]", RegexOptions.Compiled);

    public static void EnrichCoverImageUrls(IReadOnlyList<BookMetadataResult> books)
    {
        foreach (var book in books)
        {
            if (string.IsNullOrWhiteSpace(book.coverImageUrl) == false)
                book.coverImageUrl = NormalizeHttpUrl(book.coverImageUrl);
            else if (NormalizeIsbn(book.isbn) is { } isbnForCover)
                book.coverImageUrl = $"https://covers.openlibrary.org/b/isbn/{isbnForCover}-L.jpg";

            EnrichSourceUrl(book);
        }
    }

    private static void EnrichSourceUrl(BookMetadataResult book)
    {
        if (string.IsNullOrWhiteSpace(book.sourceUrl) == false)
        {
            book.sourceUrl = NormalizeHttpUrl(book.sourceUrl);
            return;
        }

        if (NormalizeIsbn(book.isbn) is not { } isbn)
            return;

        book.sourceUrl = $"https://openlibrary.org/isbn/{isbn}";
    }

    private static string? NormalizeIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return null;

        var digits = IsbnDigitsOnly.Replace(isbn.Trim(), string.Empty).ToUpperInvariant();
        return digits.Length is 10 or 13 ? digits : null;
    }

    private static string NormalizeHttpUrl(string url)
    {
        url = url.Trim();
        if (url.StartsWith("//", StringComparison.Ordinal))
            return "https:" + url;

        return url;
    }
}
