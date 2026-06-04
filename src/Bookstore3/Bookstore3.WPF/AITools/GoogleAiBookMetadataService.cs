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
        var options = GoogleAppOptions.Load(_appOptionRepository, _stringCipher);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Google API key is missing. Set it in Options (toolbar).");

        var googleAi = new GoogleAI(options.ApiKey);
        var researchNotes = await FetchWebResearchAsync(
            googleAi,
            options.Model,
            title,
            author,
            edition,
            cancellationToken);

        var jsonText = await FormatResearchAsJsonAsync(
            googleAi,
            options.Model,
            title,
            author,
            edition,
            researchNotes,
            cancellationToken);

        return BookMetadataJsonParser.ParseList(jsonText);
    }

    private static async Task<string> FetchWebResearchAsync(
        GoogleAI googleAi,
        string modelName,
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken)
    {
        var systemInstruction = new Content(BookMetadataPrompt.WebResearchSystemInstruction);
        var model = googleAi.GenerativeModel(model: modelName, systemInstruction: systemInstruction);
        model.UseGoogleSearch = true;

        var request = new GenerateContentRequest(
            BookMetadataPrompt.BuildWebResearchUserPrompt(title, author, edition))
        {
            GenerationConfig = new GenerationConfig
            {
                Temperature = 0.2f,
                MaxOutputTokens = 8192
            }
        };

        var response = await model.GenerateContent(request, cancellationToken: cancellationToken);
        return GoogleGeminiResponseText.GetTextOrThrow(response, "Google AI web research");
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
        model.UseGoogleSearch = false;

        var request = new GenerateContentRequest(
            BookMetadataPrompt.BuildJsonFromResearchUserPrompt(title, author, edition, researchNotes))
        {
            GenerationConfig = new GenerationConfig
            {
                Temperature = 0.1f,
                MaxOutputTokens = 8192,
                ResponseMimeType = "application/json",
                ResponseSchema = Schema.FromType<BookMetadataListResponse>()
            }
        };

        var response = await model.GenerateContent(request, cancellationToken: cancellationToken);
        return GoogleGeminiResponseText.GetTextOrThrow(response, "Google AI JSON formatting");
    }

    private readonly IAppOptionRepository _appOptionRepository;
    private readonly IStringCipher _stringCipher;
}
