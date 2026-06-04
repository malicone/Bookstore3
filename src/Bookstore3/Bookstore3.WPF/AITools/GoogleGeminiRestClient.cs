using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bookstore3.WPF.AITools;

/// <summary>
/// Direct Gemini REST calls for grounded search. Uses the official <c>google_search</c> tool
/// (not deprecated <c>google_search_retrieval</c>) per https://ai.google.dev/gemini-api/docs/google-search
/// </summary>
internal static class GoogleGeminiRestClient
{
    private const string GenerateContentUrlTemplate =
        "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";

    public static readonly TimeSpan SearchRequestTimeout = TimeSpan.FromMinutes(8);

    public static string ResolveSearchModel(string configuredModel)
    {
        if (GoogleGeminiGenerationConfig.IsGemini3OrLater(configuredModel))
            return GoogleAppOptionKeys.SearchModel;

        return configuredModel;
    }

    public static async Task<GoogleGeminiRestResponse> GenerateWithGoogleSearchAsync(
        string apiKey,
        string model,
        string systemInstruction,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = SearchRequestTimeout };

        var url = string.Format(GenerateContentUrlTemplate, model.Trim());
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", apiKey);

        var payload = BuildSearchRequestPayload(systemInstruction, userPrompt, model);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        GoogleAiDebugLog.Write($"REST google_search: POST models/{model} (timeout {SearchRequestTimeout.TotalMinutes:0} min)");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode == false)
        {
            GoogleAiDebugLog.WriteResponseSnippet("REST error body", body, maxLength: 2000);
            throw new InvalidOperationException(
                $"Google AI REST error ({(int)response.StatusCode}): {Truncate(body, 1500)}");
        }

        var parsed = ParseResponse(body);
        GoogleAiDebugLog.Write(
            $"REST google_search completed: text length={parsed.Text?.Length ?? 0}, " +
            $"finishReason={parsed.FinishReason}, webSearchQueryCount={parsed.WebSearchQueries.Count}, " +
            $"groundingChunkCount={parsed.GroundingChunkUrls.Count}");
        GoogleAiDebugLog.WriteGroundingMetadata("REST google_search", parsed);

        if (string.IsNullOrWhiteSpace(parsed.Text))
        {
            GoogleAiDebugLog.WriteResponseSnippet("REST empty text, raw body", body, maxLength: 2000);
            var detail = BuildEmptyTextDetail(parsed);
            throw new InvalidOperationException($"Google AI returned empty text after web search ({detail}).");
        }

        return parsed;
    }

    private static string BuildSearchRequestPayload(string systemInstruction, string userPrompt, string model)
    {
        var generationConfig = new Dictionary<string, object?>
        {
            ["temperature"] = 0.2,
            ["maxOutputTokens"] = 8192
        };

        if (GoogleGeminiGenerationConfig.IsGemini3OrLater(model))
        {
            generationConfig["thinkingConfig"] = new Dictionary<string, object?>
            {
                ["thinkingLevel"] = "LOW",
                ["includeThoughts"] = false
            };
        }

        var payload = new Dictionary<string, object?>
        {
            ["systemInstruction"] = new { parts = new[] { new { text = systemInstruction } } },
            ["contents"] = new[]
            {
                new { role = "user", parts = new[] { new { text = userPrompt } } }
            },
            ["tools"] = new[] { new { google_search = new { } } },
            ["generationConfig"] = generationConfig
        };

        return JsonSerializer.Serialize(payload, JsonSerializerOptions);
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static GoogleGeminiRestResponse ParseResponse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("error", out var error))
        {
            var message = error.TryGetProperty("message", out var msg) ? msg.GetString() : error.ToString();
            throw new InvalidOperationException($"Google AI API error: {message}");
        }

        var text = ExtractText(root);
        var finishReason = TryGetFirstCandidateFinishReason(root);
        var blockReason = TryGetPromptBlockReason(root);

        var webSearchQueries = new List<string>();
        var groundingChunkUrls = new List<string>();
        string? searchEntryPoint = null;

        if (root.TryGetProperty("candidates", out var candidates) &&
            candidates.ValueKind == JsonValueKind.Array &&
            candidates.GetArrayLength() > 0 &&
            candidates[0].TryGetProperty("groundingMetadata", out var grounding))
        {
            webSearchQueries = ExtractWebSearchQueries(grounding);
            groundingChunkUrls = ExtractGroundingChunkUrls(grounding);
            if (grounding.TryGetProperty("searchEntryPoint", out var entryPoint) &&
                entryPoint.TryGetProperty("renderedContent", out var rendered))
            {
                searchEntryPoint = rendered.GetString();
            }
        }

        return new GoogleGeminiRestResponse(
            text,
            finishReason,
            blockReason,
            webSearchQueries,
            groundingChunkUrls,
            searchEntryPoint);
    }

    private static string? TryGetFirstCandidateFinishReason(JsonElement root)
    {
        if (root.TryGetProperty("candidates", out var candidates) == false ||
            candidates.ValueKind != JsonValueKind.Array ||
            candidates.GetArrayLength() == 0)
            return null;

        return candidates[0].TryGetProperty("finishReason", out var reason)
            ? reason.GetString()
            : null;
    }

    private static string? TryGetPromptBlockReason(JsonElement root)
    {
        if (root.TryGetProperty("promptFeedback", out var feedback) == false)
            return null;

        if (feedback.TryGetProperty("blockReason", out var blockReason))
        {
            var value = blockReason.GetString();
            if (string.IsNullOrWhiteSpace(value) == false)
                return value;
        }

        return null;
    }

    private static List<string> ExtractWebSearchQueries(JsonElement grounding)
    {
        var queries = new List<string>();
        if (grounding.TryGetProperty("webSearchQueries", out var queriesElement) == false ||
            queriesElement.ValueKind != JsonValueKind.Array)
            return queries;

        foreach (var query in queriesElement.EnumerateArray())
        {
            if (query.ValueKind == JsonValueKind.String)
            {
                var value = query.GetString();
                if (string.IsNullOrWhiteSpace(value) == false)
                    queries.Add(value.Trim());
                continue;
            }

            if (query.ValueKind == JsonValueKind.Object &&
                query.TryGetProperty("query", out var queryText))
            {
                var value = queryText.GetString();
                if (string.IsNullOrWhiteSpace(value) == false)
                    queries.Add(value.Trim());
            }
        }

        return queries;
    }

    private static List<string> ExtractGroundingChunkUrls(JsonElement grounding)
    {
        var urls = new List<string>();
        if (grounding.TryGetProperty("groundingChunks", out var chunks) == false ||
            chunks.ValueKind != JsonValueKind.Array)
            return urls;

        foreach (var chunk in chunks.EnumerateArray())
        {
            if (chunk.TryGetProperty("web", out var web) == false)
                continue;

            if (web.TryGetProperty("uri", out var uri))
            {
                var value = uri.GetString();
                if (string.IsNullOrWhiteSpace(value) == false)
                    urls.Add(value.Trim());
            }
            else if (web.TryGetProperty("title", out var title))
            {
                var value = title.GetString();
                if (string.IsNullOrWhiteSpace(value) == false)
                    urls.Add($"(title only) {value.Trim()}");
            }
        }

        return urls;
    }

    private static string? ExtractText(JsonElement root)
    {
        if (root.TryGetProperty("candidates", out var candidates) == false ||
            candidates.ValueKind != JsonValueKind.Array)
            return null;

        var text = CollectTextFromCandidates(candidates, includeThoughtParts: false);
        if (string.IsNullOrWhiteSpace(text) == false)
            return text;

        text = CollectTextFromCandidates(candidates, includeThoughtParts: true);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string? CollectTextFromCandidates(JsonElement candidates, bool includeThoughtParts)
    {
        var sb = new StringBuilder();
        var index = 0;

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (candidate.TryGetProperty("content", out var content) == false)
            {
                if (includeThoughtParts)
                    LogCandidateWithoutContent(candidate, index);

                index++;
                continue;
            }

            if (content.TryGetProperty("parts", out var parts) == false ||
                parts.ValueKind != JsonValueKind.Array)
            {
                index++;
                continue;
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (includeThoughtParts == false &&
                    part.TryGetProperty("thought", out var thoughtFlag) &&
                    thoughtFlag.ValueKind == JsonValueKind.True)
                    continue;

                if (part.TryGetProperty("text", out var textElement) == false)
                    continue;

                var text = textElement.GetString();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (sb.Length > 0)
                    sb.AppendLine();

                sb.Append(text);
            }

            index++;
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static void LogCandidateWithoutContent(JsonElement candidate, int index)
    {
        var propertyNames = candidate.EnumerateObject().Select(property => property.Name).ToList();
        var finishReason = candidate.TryGetProperty("finishReason", out var reason)
            ? reason.GetString()
            : null;
        GoogleAiDebugLog.Write(
            $"REST candidate[{index}] has no content (finishReason={finishReason ?? "unknown"}, " +
            $"properties={string.Join(", ", propertyNames)}).");
    }

    private static string BuildEmptyTextDetail(GoogleGeminiRestResponse parsed)
    {
        var details = new List<string>();
        if (string.IsNullOrWhiteSpace(parsed.BlockReason) == false)
            details.Add($"prompt blocked ({parsed.BlockReason})");
        if (string.IsNullOrWhiteSpace(parsed.FinishReason) == false)
            details.Add($"finishReason={parsed.FinishReason}");

        return details.Count > 0
            ? string.Join("; ", details)
            : "no extractable candidate text";
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}

internal sealed record GoogleGeminiRestResponse(
    string? Text,
    string? FinishReason,
    string? BlockReason,
    IReadOnlyList<string> WebSearchQueries,
    IReadOnlyList<string> GroundingChunkUrls,
    string? SearchEntryPoint);
