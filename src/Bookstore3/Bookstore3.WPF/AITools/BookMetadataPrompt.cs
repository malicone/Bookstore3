namespace Bookstore3.WPF.AITools;

internal static class BookMetadataPrompt
{
    public const string SystemInstruction =
        "You are a strict JSON API. Return only a single JSON object. " +
        "If value is unknown set null. Do not hallucinate. " +
        "Search the public web for fresh, up-to-date information before answering. " +
        "Use only publicly available information. " +
        "Prefer current publisher pages, ISBN databases, library catalogs, and official book listings over memorized knowledge.";

    public const string WebResearchSystemInstruction =
        "You are a book metadata researcher. Search the public web for fresh, up-to-date information. " +
        "Use only publicly available sources such as publisher pages, ISBN databases, library catalogs, and bookstore listings. " +
        "Do not hallucinate. If a value is unknown, say so. " +
        "For summaries, use only publicly available summary text; paraphrase briefly if needed to avoid copying protected text verbatim.";

    public const string JsonFromResearchSystemInstruction =
        "You are a strict JSON API. Convert the supplied research notes into a single JSON object with a books array. " +
        "Include every distinct book described in the notes. If a value is unknown set null. " +
        "When the search criteria title or author match a book in the notes, always include that book with at least title and author filled in.";

    private const string BooksCountRequirement =
        "Find at least 3 books and up to 10 books (maximum limit 10). " +
        "Include every distinct relevant match in that range; do not stop after the first book. ";

    public static string BuildUserPrompt(string title, string? author, int? edition)
    {
        var searchCriteria = BuildSearchCriteria(title, author, edition);

        return
            "Go to the web and search for books matching the criteria below. " +
            BooksCountRequirement +
            "Return one JSON object with a single property \"books\" whose value is an array of book objects " +
            "ordered from most relevant to least relevant. " +
            "Each book object must contain only these nullable fields: " +
            MetadataFieldList + " " +
            FormatFieldExplanation +
            HardcoverFieldExplanation +
            CoverImageUrlFieldExplanation +
            SourceUrlFieldExplanation +
            "The annotation field is the book summary (annotation is a synonym of summary). " +
            "For annotation, search and use only publicly available summary text about the book from the web " +
            "(for example library catalogs, Open Library, ISBN databases, publisher pages, or bookstore listings). " +
            "Do not use private, paywalled, or non-public sources. Paraphrase briefly if needed to avoid copying protected text verbatim. " +
            "publishYear must be integer year if known. URLs must be absolute https URLs. " +
            "Return only raw JSON with no markdown, no code fences, and no extra text. " +
            "If no matches are found return {\"books\":[]}. " +
            searchCriteria;
    }

    public static string BuildWebResearchUserPrompt(string title, string? author, int? edition)
    {
        return
            "Search the web for books matching the criteria below. " +
            BooksCountRequirement +
            "Write plain-text research notes for each match, " +
            "ordered from most relevant to least relevant. " +
            "For each book include whatever you can verify from public sources: " +
            MetadataFieldList + " " +
            FormatFieldExplanation +
            HardcoverFieldExplanation +
            CoverImageUrlFieldExplanation +
            SourceUrlFieldExplanation +
            "The annotation field is the book summary. Use only publicly available summary text; paraphrase briefly if needed. " +
            "If no matches are found, say that no matches were found. " +
            "Do not return JSON. " +
            BuildSearchCriteria(title, author, edition);
    }

    public static string BuildJsonFromResearchUserPrompt(string title, string? author, int? edition, string researchNotes)
    {
        return
            "Convert the research notes below into one JSON object with a single property \"books\" " +
            "whose value is an array of book objects ordered from most relevant to least relevant. " +
            BooksCountRequirement +
            "Each book object must contain only these nullable fields: " +
            MetadataFieldList + " " +
            FormatFieldExplanation +
            HardcoverFieldExplanation +
            CoverImageUrlFieldExplanation +
            SourceUrlFieldExplanation +
            "publishYear must be integer year if known. " +
            "Only return {\"books\":[]} when the notes explicitly state that no books were found. " +
            BuildSearchCriteria(title, author, edition) +
            "\n\nResearch notes:\n" +
            researchNotes.Trim();
    }

    private const string FormatFieldExplanation =
        "The format field is the same as Dimensions: physical book size, not binding type " +
        "(for example 70x100/16, or height and width in cm or inches such as 23 x 15 cm). Do not use Paperback or Hardcover for format. ";

    private const string HardcoverFieldExplanation =
        "The wrapper field is the Hardcover book parameter (boolean). " +
        "Set wrapper to true if Hardcover is mentioned in the found book description. " +
        "Set wrapper to false if Paperback is mentioned in the book description. " +
        "Set wrapper to null if neither binding is stated or binding is unknown. ";

    private const string CoverImageUrlFieldExplanation =
        "The coverImageUrl field must be a direct https URL to the book cover image file (jpg, png, or webp), " +
        "not a bookstore or catalog page URL. " +
        "Search for cover art on publisher pages, ISBN databases, and bookstore listings. ";

    private const string SourceUrlFieldExplanation =
        "The sourceUrl field is the https URL of the public web page where this book's metadata was found " +
        "(publisher page, library catalog, ISBN database, bookstore listing). " +
        "It must be a catalog or listing page URL, not a cover image URL. Don't generate the sourceUrl but use actual from the Web. ";

    private const string MetadataFieldList =
        "title, author, isbn, pageCount, edition, format, publishYear, price, group, language, publisher, city, " +
        "wrapper, annotation, coverImageUrl, sourceUrl.";

    private static string BuildSearchCriteria(string title, string? author, int? edition)
    {
        var searchCriteria = $"Book title: {title}";
        if (string.IsNullOrWhiteSpace(author) == false)
            searchCriteria += $"\nAuthor: {author.Trim()}";
        if (edition.HasValue)
            searchCriteria += $"\nEdition: {edition.Value}";

        return searchCriteria;
    }
}