using Bookstore3.Repository;
using Bookstore3.WPF.Utils;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;

namespace Bookstore3.WPF.AITools;

internal sealed class GoogleAiBookMetadataService : IAiBookMetadataService
{
    public GoogleAiBookMetadataService(
        IAppOptionRepository appOptionRepository,
        IStringCipher stringCipher,
        IAiDebugLog? debugLog = null)
    {
        _appOptionRepository = appOptionRepository;
        _stringCipher = stringCipher;
        _debugLog = debugLog ?? GoogleAiDebugLog.Instance;
    }

    public async Task<IReadOnlyList<BookMetadataResult>> FetchMetadataAsync(
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken)
    {
        _debugLog.BeginSession(
            $"FetchMetadataAsync — title=\"{title}\", author=\"{author ?? "(none)"}\", edition={edition?.ToString() ?? "(none)"}");

        var options = GoogleAppOptions.Load(_appOptionRepository, _stringCipher);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Google API key is missing. Set it in Options.");

        _debugLog.Write(
            $"Options: model=\"{options.Model}\", searchModel=\"{GoogleGeminiRestClient.ResolveSearchModel(options.Model)}\", apiKey length={options.ApiKey.Length}");

        var googleAi = new GoogleAI(options.ApiKey);
        var modelName = options.Model.Trim();
        var apiKey = options.ApiKey;
        var errors = new List<string>();

        foreach (var (strategyName, strategy) in BuildStrategies())
        {
            _debugLog.Write($"Trying strategy: {strategyName}");
            try
            {
                var results = await strategy(googleAi, apiKey, modelName, title, author, edition, cancellationToken);
                _debugLog.Write($"Strategy {strategyName} finished — parsed book count={results.Count}");
                if (results.Count > 0)
                {
                    LogBookResults(results);
                    return results;
                }

                _debugLog.Write($"Strategy {strategyName} returned 0 books, trying next.");
            }
            catch (Exception ex)
            {
                _debugLog.WriteException($"Strategy {strategyName}", ex);
                errors.Add($"{strategyName}: {GoogleGeminiApiHelper.FormatExceptionForDialog(ex)}");
            }
        }

        var detail = errors.Count > 0
            ? string.Join($"{Environment.NewLine}{Environment.NewLine}", errors.Distinct())
            : "Every strategy returned 0 books without throwing.";
        _debugLog.Write("All strategies failed. Throwing.");
        throw new InvalidOperationException(
            $"Google AI could not find books for \"{title}\".{Environment.NewLine}{Environment.NewLine}{detail}");
    }

    private void LogBookResults(IReadOnlyList<BookMetadataResult> results)
    {
        for (var i = 0; i < Math.Min(results.Count, 3); i++)
        {
            _debugLog.Write(
                $"  book[{i}]: title=\"{results[i].title}\", author=\"{results[i].author}\", " +
                $"isbn=\"{results[i].isbn}\", coverImageUrl=\"{results[i].coverImageUrl ?? "(null)"}\"");
        }
    }

    private void LogPrompt(string contextLabel, string systemInstruction, string userPrompt)
    {
        _debugLog.WriteResponseSnippet($"{contextLabel} system instruction", systemInstruction, maxLength: 500);
        _debugLog.WriteResponseSnippet($"{contextLabel} user prompt", userPrompt, maxLength: 2000);
    }

    private IEnumerable<(string Name, StrategyDelegate Strategy)> BuildStrategies()
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

    private async Task<IReadOnlyList<BookMetadataResult>> FetchWithGoogleSearchRestAsync(
        GoogleAI googleAi,
        string apiKey,
        string modelName,
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken)
    {
        var searchModel = GoogleGeminiRestClient.ResolveSearchModel(modelName);
        _debugLog.Write($"SearchRest: configured model={modelName}, grounding model={searchModel}");

        var systemInstruction = BookMetadataPrompt.SystemInstruction;
        var userPrompt = BookMetadataPrompt.BuildUserPrompt(title, author, edition);
        LogPrompt("SearchRest", systemInstruction, userPrompt);

        var restResponse = await GoogleGeminiRestClient.GenerateWithGoogleSearchAsync(
            apiKey,
            searchModel,
            systemInstruction,
            userPrompt,
            cancellationToken);

        _debugLog.WriteResponseSnippet("SearchRest text", restResponse.Text);
        return BookMetadataJsonParser.ParseList(restResponse.Text!, _debugLog, "SearchRest");
    }

    private async Task<IReadOnlyList<BookMetadataResult>> FetchJsonWithoutSearchAsync(
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

    private async Task<IReadOnlyList<BookMetadataResult>> GenerateAndParseWithSdkAsync(
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
        var systemInstructionText = BookMetadataPrompt.SystemInstruction;
        var userPrompt = BookMetadataPrompt.BuildUserPrompt(title, author, edition);
        LogPrompt(contextLabel, systemInstructionText, userPrompt);

        var systemInstruction = new Content(systemInstructionText);
        var model = googleAi.GenerativeModel(model: modelName, systemInstruction: systemInstruction);
        GoogleGeminiApiHelper.ConfigureModel(model, useGoogleSearch);
        model.UseGoogleSearch = false;

        _debugLog.Write($"SDK Generate ({contextLabel}): model={modelName}, jsonResponse={jsonResponse}");

        var request = new GenerateContentRequest(userPrompt)
        {
            GenerationConfig = GoogleGeminiGenerationConfig.ForMetadataFetch(modelName, jsonResponse)
        };

        var response = await GoogleGeminiApiHelper.GenerateContentAsync(
            model,
            request,
            contextLabel,
            useGoogleSearch: false,
            _debugLog,
            cancellationToken);

        LogSdkResponse(contextLabel, response);

        var jsonText = GoogleGeminiResponseText.GetTextOrThrow(response, _debugLog, $"Google AI ({contextLabel})");
        _debugLog.WriteResponseSnippet($"{contextLabel} text", jsonText);
        return BookMetadataJsonParser.ParseList(jsonText, _debugLog, contextLabel);
    }

    private async Task<IReadOnlyList<BookMetadataResult>> FetchWithJsonFormattingFallbackAsync(
        GoogleAI googleAi,
        string apiKey,
        string modelName,
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken)
    {
        var searchModel = GoogleGeminiRestClient.ResolveSearchModel(modelName);
        var webResearchSystemInstruction = BookMetadataPrompt.WebResearchSystemInstruction;
        var webResearchUserPrompt = BookMetadataPrompt.BuildWebResearchUserPrompt(title, author, edition);
        LogPrompt("web research", webResearchSystemInstruction, webResearchUserPrompt);

        var restResearch = await GoogleGeminiRestClient.GenerateWithGoogleSearchAsync(
            apiKey,
            searchModel,
            webResearchSystemInstruction,
            webResearchUserPrompt,
            cancellationToken);

        _debugLog.WriteResponseSnippet("web research REST text", restResearch.Text);

        var jsonText = await FormatResearchAsJsonAsync(
            googleAi,
            modelName,
            title,
            author,
            edition,
            restResearch.Text!,
            cancellationToken);

        return BookMetadataJsonParser.ParseList(jsonText, _debugLog, "research then JSON");
    }

    private async Task<string> FormatResearchAsJsonAsync(
        GoogleAI googleAi,
        string modelName,
        string title,
        string? author,
        int? edition,
        string researchNotes,
        CancellationToken cancellationToken)
    {
        var systemInstructionText = BookMetadataPrompt.JsonFromResearchSystemInstruction;
        var userPrompt = BookMetadataPrompt.BuildJsonFromResearchUserPrompt(title, author, edition, researchNotes);
        LogPrompt("JSON formatting", systemInstructionText, userPrompt);

        var systemInstruction = new Content(systemInstructionText);
        var model = googleAi.GenerativeModel(model: modelName, systemInstruction: systemInstruction);
        GoogleGeminiApiHelper.ConfigureModel(model, useGoogleSearch: false);
        model.UseGoogleSearch = false;

        var request = new GenerateContentRequest(userPrompt)
        {
            GenerationConfig = GoogleGeminiGenerationConfig.ForMetadataFetch(modelName, jsonResponse: true)
        };

        var response = await GoogleGeminiApiHelper.GenerateContentAsync(
            model,
            request,
            "JSON formatting",
            useGoogleSearch: false,
            _debugLog,
            cancellationToken);

        LogSdkResponse("JSON formatting", response);

        var text = GoogleGeminiResponseText.GetTextOrThrow(response, _debugLog, "Google AI JSON formatting");
        _debugLog.WriteResponseSnippet("JSON formatting text", text);
        return text;
    }

    private void LogSdkResponse(string label, GenerateContentResponse? response)
    {
        if (response is null)
        {
            _debugLog.Write($"{label} response: null");
            return;
        }

        var candidate = response.Candidates?.FirstOrDefault();
        _debugLog.Write(
            $"{label} response: Text length={response.Text?.Length ?? 0}, " +
            $"candidates={response.Candidates?.Count ?? 0}, " +
            $"finishReason={candidate?.FinishReason}, " +
            $"blockReason={response.PromptFeedback?.BlockReason}");
    }

    private readonly IAppOptionRepository _appOptionRepository;
    private readonly IStringCipher _stringCipher;
    private readonly IAiDebugLog _debugLog;
}
