using Bookstore3.Repository;
using Bookstore3.WPF.Utils;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Bookstore3.WPF.AITools;

internal sealed class OpenAiBookMetadataService : IAiBookMetadataService
{
    public OpenAiBookMetadataService(IAppOptionRepository appOptionRepository, IStringCipher stringCipher)
    {
        _appOptionRepository = appOptionRepository;
        _stringCipher = stringCipher;
    }

    public async Task<BookMetadataResult> FetchMetadataAsync(
        string title,
        string? author,
        CancellationToken cancellationToken)
    {
        var options = OpenAiAppOptions.Load(_appOptionRepository, _stringCipher);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Open AI API key is missing. Set it in Options (toolbar).");

        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new InvalidOperationException("Open AI endpoint is missing. Set it in Options (toolbar).");

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
                new { role = "user", content = BookMetadataPrompt.BuildUserPrompt(title, author) }
            }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode == false)
            throw new InvalidOperationException($"OpenAI request failed ({(int)response.StatusCode}): {body}");

        var completion = System.Text.Json.JsonSerializer.Deserialize<ChatCompletionResponse>(body, BookMetadataJsonParserJsonOptions)
                         ?? throw new InvalidOperationException("OpenAI returned an invalid response.");
        var content = completion.choices?.FirstOrDefault()?.message?.content;
        return BookMetadataJsonParser.Parse(content ?? string.Empty);
    }

    private readonly IAppOptionRepository _appOptionRepository;
    private readonly IStringCipher _stringCipher;
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private static readonly System.Text.Json.JsonSerializerOptions BookMetadataJsonParserJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class ChatCompletionResponse
    {
        public List<Choice>? choices { get; set; }
    }

    private sealed class Choice
    {
        public Message? message { get; set; }
    }

    private sealed class Message
    {
        public string? content { get; set; }
    }
}
