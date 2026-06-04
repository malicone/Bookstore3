using Bookstore3.Repository;
using Bookstore3.WPF.Utils;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;

namespace Bookstore3.WPF.AITools;

internal sealed class GoogleAiBookMetadataService : IAiBookMetadataService
{
    public GoogleAiBookMetadataService(IAppOptionRepository appOptionRepository, IStringCipher stringCipher)
    {
        _appOptionRepository = appOptionRepository;
        _stringCipher = stringCipher;
    }

    public async Task<IReadOnlyList<BookMetadataResult>> FetchMetadataAsync(
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken)
    {
        GoogleAiDebugLog.BeginSession(
            $"FetchMetadataAsync — title=\"{title}\", author=\"{author ?? "(none)"}\", edition={edition?.ToString() ?? "(none)"}");

        var options = GoogleAppOptions.Load(_appOptionRepository, _stringCipher);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Google API key is missing. Set it in Options.");

        GoogleAiDebugLog.Write(
            $"Options: model=\"{options.Model}\", searchModel=\"{GoogleGeminiRestClient.ResolveSearchModel(options.Model)}\", apiKey length={options.ApiKey.Length}");

        var googleAi = new GoogleAI(options.ApiKey);
        var modelName = options.Model.Trim();
        var apiKey = options.ApiKey;
        var errors = new List<string>();

        foreach (var (strategyName, strategy) in BuildStrategies())
        {
            GoogleAiDebugLog.Write($"Trying strategy: {strategyName}");
            try
            {
                var results = await strategy(googleAi, apiKey, modelName, title, author, edition, cancellationToken);
                GoogleAiDebugLog.Write($"Strategy {strategyName} finished — parsed book count={results.Count}");
                if (results.Count > 0)
                {
                    for (var i = 0; i < Math.Min(results.Count, 3); i++)
                        GoogleAiDebugLog.Write(
                            $"  book[{i}]: title=\"{results[i].title}\", author=\"{results[i].author}\", " +
                            $"isbn=\"{results[i].isbn}\", coverImageUrl=\"{results[i].coverImageUrl ?? "(null)"}\"");
                    return results;
                }

                GoogleAiDebugLog.Write($"Strategy {strategyName} returned 0 books, trying next.");
            }
            catch (Exception ex)
            {
                GoogleAiDebugLog.WriteException($"Strategy {strategyName}", ex);
                errors.Add($"{strategyName}: {GoogleGeminiApiHelper.FormatExceptionForDialog(ex)}");
            }
        }

        var detail = errors.Count > 0
            ? string.Join($"{Environment.NewLine}{Environment.NewLine}", errors.Distinct())
            : "Every strategy returned 0 books without throwing.";
        GoogleAiDebugLog.Write("All strategies failed. Throwing.");
        throw new InvalidOperationException(
            $"Google AI could not find books for \"{title}\".{Environment.NewLine}{Environment.NewLine}{detail}");
    }

    private static IEnumerable<(string Name, StrategyDelegate Strategy)> BuildStrategies()
    {
        yield return ("SearchRest", FetchWithGoogleSearchRestAsync);
        yield return ("JsonNoSearch", FetchJsonWithoutSearchAsync);
        yield return ("ResearchThenJson", FetchWithJsonFormattingFallbackAsync);
    }

    private delegate Task<IReadOnlyList<BookMetadataResult>> StrategyDelegate(
        GoogleAI googleAi,
        string apiKey,
        string modelName,
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken);

    private static async Task<IReadOnlyList<BookMetadataResult>> FetchWithGoogleSearchRestAsync(
        GoogleAI googleAi,
        string apiKey,
        string modelName,
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken)
    {
        var searchModel = GoogleGeminiRestClient.ResolveSearchModel(modelName);
        GoogleAiDebugLog.Write($"SearchRest: configured model={modelName}, grounding model={searchModel}");

        var restResponse = await GoogleGeminiRestClient.GenerateWithGoogleSearchAsync(
            apiKey,
            searchModel,
            BookMetadataPrompt.SystemInstruction,
            BookMetadataPrompt.BuildUserPrompt(title, author, edition),
            cancellationToken);

        GoogleAiDebugLog.WriteResponseSnippet("SearchRest text", restResponse.Text);
        return BookMetadataJsonParser.ParseList(restResponse.Text!, "SearchRest");
    }

    private static async Task<IReadOnlyList<BookMetadataResult>> FetchJsonWithoutSearchAsync(
        GoogleAI googleAi,
        string apiKey,
        string modelName,
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken) =>
        await GenerateAndParseWithSdkAsync(
            googleAi,
            modelName,
            title,
            author,
            edition,
            useGoogleSearch: false,
            jsonResponse: true,
            contextLabel: "JSON only",
            cancellationToken);

    private static async Task<IReadOnlyList<BookMetadataResult>> GenerateAndParseWithSdkAsync(
        GoogleAI googleAi,
        string modelName,
        string title,
        string? author,
        int? edition,
        bool useGoogleSearch,
        bool jsonResponse,
        string contextLabel,
        CancellationToken cancellationToken)
    {
        var systemInstruction = new Content(BookMetadataPrompt.SystemInstruction);
        var model = googleAi.GenerativeModel(model: modelName, systemInstruction: systemInstruction);
        GoogleGeminiApiHelper.ConfigureModel(model, useGoogleSearch);
        model.UseGoogleSearch = false;

        GoogleAiDebugLog.Write($"SDK Generate ({contextLabel}): model={modelName}, jsonResponse={jsonResponse}");

        var request = new GenerateContentRequest(BookMetadataPrompt.BuildUserPrompt(title, author, edition))
        {
            GenerationConfig = GoogleGeminiGenerationConfig.ForMetadataFetch(modelName, jsonResponse)
        };

        var response = await GoogleGeminiApiHelper.GenerateContentAsync(
            model,
            request,
            contextLabel,
            useGoogleSearch: false,
            cancellationToken);

        LogSdkResponse(contextLabel, response);

        var jsonText = GoogleGeminiResponseText.GetTextOrThrow(response, $"Google AI ({contextLabel})");
        GoogleAiDebugLog.WriteResponseSnippet($"{contextLabel} text", jsonText);
        return BookMetadataJsonParser.ParseList(jsonText, contextLabel);
    }

    private static async Task<IReadOnlyList<BookMetadataResult>> FetchWithJsonFormattingFallbackAsync(
        GoogleAI googleAi,
        string apiKey,
        string modelName,
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken)
    {
        var searchModel = GoogleGeminiRestClient.ResolveSearchModel(modelName);
        var restResearch = await GoogleGeminiRestClient.GenerateWithGoogleSearchAsync(
            apiKey,
            searchModel,
            BookMetadataPrompt.WebResearchSystemInstruction,
            BookMetadataPrompt.BuildWebResearchUserPrompt(title, author, edition),
            cancellationToken);

        GoogleAiDebugLog.WriteResponseSnippet("web research REST text", restResearch.Text);

        var jsonText = await FormatResearchAsJsonAsync(
            googleAi,
            modelName,
            title,
            author,
            edition,
            restResearch.Text!,
            cancellationToken);

        return BookMetadataJsonParser.ParseList(jsonText, "research then JSON");
    }

    private static async Task<string> FormatResearchAsJsonAsync(
        GoogleAI googleAi,
        string modelName,
        string title,
        string? author,
        int? edition,
        string researchNotes,
        CancellationToken cancellationToken)
    {
        var systemInstruction = new Content(BookMetadataPrompt.JsonFromResearchSystemInstruction);
        var model = googleAi.GenerativeModel(model: modelName, systemInstruction: systemInstruction);
        GoogleGeminiApiHelper.ConfigureModel(model, useGoogleSearch: false);
        model.UseGoogleSearch = false;

        var request = new GenerateContentRequest(
            BookMetadataPrompt.BuildJsonFromResearchUserPrompt(title, author, edition, researchNotes))
        {
            GenerationConfig = GoogleGeminiGenerationConfig.ForMetadataFetch(modelName, jsonResponse: true)
        };

        var response = await GoogleGeminiApiHelper.GenerateContentAsync(
            model,
            request,
            "JSON formatting",
            useGoogleSearch: false,
            cancellationToken);

        LogSdkResponse("JSON formatting", response);

        var text = GoogleGeminiResponseText.GetTextOrThrow(response, "Google AI JSON formatting");
        GoogleAiDebugLog.WriteResponseSnippet("JSON formatting text", text);
        return text;
    }

    private static void LogSdkResponse(string label, GenerateContentResponse? response)
    {
        if (response is null)
        {
            GoogleAiDebugLog.Write($"{label} response: null");
            return;
        }

        var candidate = response.Candidates?.FirstOrDefault();
        GoogleAiDebugLog.Write(
            $"{label} response: Text length={response.Text?.Length ?? 0}, " +
            $"candidates={response.Candidates?.Count ?? 0}, " +
            $"finishReason={candidate?.FinishReason}, " +
            $"blockReason={response.PromptFeedback?.BlockReason}");
    }

    private readonly IAppOptionRepository _appOptionRepository;
    private readonly IStringCipher _stringCipher;
}
