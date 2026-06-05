using Bookstore3.Repository;
using Bookstore3.WPF.Utils;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bookstore3.WPF.AITools;

internal sealed class OpenAiBookMetadataService : IAiBookMetadataService
{
    public OpenAiBookMetadataService(
        IAppOptionRepository appOptionRepository,
        IStringCipher stringCipher,
        IAiDebugLog? debugLog = null)
    {
        _appOptionRepository = appOptionRepository;
        _stringCipher = stringCipher;
        _debugLog = debugLog ?? OpenAiDebugLog.Instance;
    }

    public async Task<IReadOnlyList<BookMetadataResult>> FetchMetadataAsync(
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken)
    {
        _debugLog.BeginSession(
            $"FetchMetadataAsync — title=\"{title}\", author=\"{author ?? "(none)"}\", edition={edition?.ToString() ?? "(none)"}");

        var options = OpenAiAppOptions.Load(_appOptionRepository, _stringCipher);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Open AI API key is missing. Set it in Options.");

        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new InvalidOperationException("Open AI endpoint is missing. Set it in Options.");

        _debugLog.Write(
            $"Options: model=\"{options.Model}\", endpoint=\"{options.Endpoint.Trim()}\", apiKey length={options.ApiKey.Length}");

        var userPrompt = BookMetadataPrompt.BuildUserPrompt(title, author, edition);
        _debugLog.Write($"Request: POST chat/completions, temperature=0.2, response_format=json_object");
        _debugLog.WriteResponseSnippet("system instruction", BookMetadataPrompt.SystemInstruction, maxLength: 500);
        _debugLog.WriteResponseSnippet("user prompt", userPrompt, maxLength: 2000);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, options.Endpoint.Trim());
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            request.Content = JsonContent.Create(new
            {
                model = options.Model,
                response_format = new { type = "json_object" },
                temperature = 0.2,
                messages = new object[]
                {
                    new { role = "system", content = BookMetadataPrompt.SystemInstruction },
                    new { role = "user", content = userPrompt }
                }
            });

            _debugLog.Write($"HTTP request starting (timeout {_httpClient.Timeout.TotalSeconds:0}s)...");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            _debugLog.Write($"HTTP response: status={(int)response.StatusCode} {response.ReasonPhrase}, body length={body.Length}");

            if (response.IsSuccessStatusCode == false)
            {
                _debugLog.WriteResponseSnippet("error body", body, maxLength: 2000);
                throw new InvalidOperationException($"OpenAI request failed ({(int)response.StatusCode}): {body}");
            }

            var completion = System.Text.Json.JsonSerializer.Deserialize<ChatCompletionResponse>(body, JsonSerializerOptions)
                             ?? throw new InvalidOperationException("OpenAI returned an invalid response.");

            var choice = completion.choices?.FirstOrDefault();
            _debugLog.Write(
                $"Completion: choices={completion.choices?.Count ?? 0}, " +
                $"finish_reason={choice?.finish_reason ?? "(none)"}, " +
                $"model={completion.model ?? "(unknown)"}");

            var content = choice?.message?.content;
            _debugLog.WriteResponseSnippet("completion content", content);

            var results = BookMetadataJsonParser.ParseList(content ?? string.Empty, _debugLog, "OpenAI");
            _debugLog.Write($"Parsed book count={results.Count}");
            LogBookResults(results);
            return results;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _debugLog.WriteException("FetchMetadataAsync", ex);
            throw;
        }
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

    private readonly IAppOptionRepository _appOptionRepository;
    private readonly IStringCipher _stringCipher;
    private readonly IAiDebugLog _debugLog;
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };

    private static readonly System.Text.Json.JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class ChatCompletionResponse
    {
        public List<Choice>? choices { get; set; }
        public string? model { get; set; }
    }

    private sealed class Choice
    {
        public Message? message { get; set; }
        public string? finish_reason { get; set; }
    }

    private sealed class Message
    {
        public string? content { get; set; }
    }
}
