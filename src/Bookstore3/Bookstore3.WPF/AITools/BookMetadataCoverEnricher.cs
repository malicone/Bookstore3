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
            {
                book.coverImageUrl = NormalizeImageUrl(book.coverImageUrl);
                continue;
            }

            var isbn = NormalizeIsbn(book.isbn);
            if (isbn is null)
                continue;

            book.coverImageUrl = $"https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg";
        }
    }

    private static string? NormalizeIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return null;

        var digits = IsbnDigitsOnly.Replace(isbn.Trim(), string.Empty).ToUpperInvariant();
        return digits.Length is 10 or 13 ? digits : null;
    }

    private static string NormalizeImageUrl(string url)
    {
        url = url.Trim();
        if (url.StartsWith("//", StringComparison.Ordinal))
            return "https:" + url;

        return url;
    }
}
