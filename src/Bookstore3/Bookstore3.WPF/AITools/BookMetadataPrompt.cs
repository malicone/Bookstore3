namespace Bookstore3.WPF.AITools;

internal static class BookMetadataPrompt
{
    public const string SystemInstruction =
        "You are a strict JSON API. Return only a single JSON object. " +
        "If value is unknown set null. Do not hallucinate. " +
        "Search the public web for fresh, up-to-date information before answering. " +
        "Prefer current publisher pages, ISBN databases, library catalogs, and official book listings over memorized knowledge.";

    public const string WebResearchSystemInstruction =
        "You look up books on the public web. Return factual notes in plain text only. Do not return JSON.";

    public static string BuildUserPrompt(string title, string? author)
    {
        var searchCriteria = BuildSearchCriteria(title, author);

        return
            "Go to the web, run a search for this book, and use the freshest reliable data you find. " +
            "Fetch metadata for the book search criteria below and return JSON with these nullable fields only: " +
            MetadataFieldList + " " +
            "The annotation field is the book summary (annotation is a synonym of summary). " +
            "publishYear must be integer year if known. URLs must be absolute https URLs. " +
            "Return only one raw JSON object with no markdown, no code fences, and no extra text. " +
            searchCriteria;
    }

    public static string BuildWebResearchUserPrompt(string title, string? author)
    {
        return
            "Search the public web for this book and write plain-text research notes. " +
            "Include every field you can verify: title, author, isbn, pageCount, edition, format, publishYear, " +
            "price, group, language, publisher, city, annotation (summary), coverImageUrl. " +
            "Use labeled lines. Set unknown fields to unknown. Do not invent data. " +
            BuildSearchCriteria(title, author);
    }

    public static string BuildJsonFromResearchUserPrompt(string title, string? author, string researchNotes)
    {
        return
            "Using only the research notes below, return one raw JSON object with these nullable fields only: " +
            MetadataFieldList + " " +
            "The annotation field is the book summary. publishYear must be integer year if known. " +
            "URLs must be absolute https URLs. No markdown, no code fences, no extra text. " +
            BuildSearchCriteria(title, author) +
            "\n\nResearch notes:\n" +
            researchNotes.Trim();
    }

    private const string MetadataFieldList =
        "title, author, isbn, pageCount, edition, format, publishYear, price, group, language, publisher, city, " +
        "annotation, coverImageUrl.";

    private static string BuildSearchCriteria(string title, string? author)
    {
        var searchCriteria = $"Book title: {title}";
        if (string.IsNullOrWhiteSpace(author) == false)
            searchCriteria += $"\nAuthor: {author.Trim()}";

        return searchCriteria;
    }
}
