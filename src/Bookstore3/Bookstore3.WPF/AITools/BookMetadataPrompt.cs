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
        "You are a strict JSON API. Convert the supplied research notes into a single JSON object. " +
        "If a value is unknown set null. Do not invent fields or values not supported by the notes.";

    public static string BuildUserPrompt(string title, string? author, int? edition)
    {
        var searchCriteria = BuildSearchCriteria(title, author, edition);

        return
            "Go to the web and search for books matching the criteria below. " +
            "Find the most relevant matches (up to 10). " +
            "Return one JSON object with a single property \"books\" whose value is an array of book objects " +
            "ordered from most relevant to least relevant. " +
            "Each book object must contain only these nullable fields: " +
            MetadataFieldList + " " +
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
            "Find the most relevant matches (up to 10) and write plain-text research notes for each match, " +
            "ordered from most relevant to least relevant. " +
            "For each book include whatever you can verify from public sources: " +
            MetadataFieldList + " " +
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
            "Each book object must contain only these nullable fields: " +
            MetadataFieldList + " " +
            "publishYear must be integer year if known. URLs must be absolute https URLs. " +
            "If the notes contain no matches return {\"books\":[]}. " +
            BuildSearchCriteria(title, author, edition) +
            "\n\nResearch notes:\n" +
            researchNotes.Trim();
    }

    private const string MetadataFieldList =
        "title, author, isbn, pageCount, edition, format, publishYear, price, group, language, publisher, city, " +
        "annotation, coverImageUrl.";

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
