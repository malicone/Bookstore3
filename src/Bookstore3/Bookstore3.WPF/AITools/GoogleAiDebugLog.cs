namespace Bookstore3.WPF.AITools;

internal sealed class GoogleAiDebugLog : AiFileDebugLog
{
    public static GoogleAiDebugLog Instance { get; } = new();

    private GoogleAiDebugLog()
    {
    }

    protected override string LogFileName => "google-ai-debug.log";

    protected override string LogPrefix => "GoogleAI";

    public void WriteGroundingMetadata(string label, GoogleGeminiRestResponse response)
    {
        Write($"{label} grounding: webSearchQueryCount={response.WebSearchQueries.Count}, groundingChunkCount={response.GroundingChunkUrls.Count}");

        if (response.WebSearchQueries.Count == 0)
            Write($"{label} webSearchQueries: (none — model may not have run web search)");
        else
        {
            for (var i = 0; i < response.WebSearchQueries.Count; i++)
                Write($"{label} webSearchQueries[{i}]: {response.WebSearchQueries[i]}");
        }

        if (response.GroundingChunkUrls.Count == 0)
            Write($"{label} groundingChunks: (none)");
        else
        {
            var maxChunks = Math.Min(response.GroundingChunkUrls.Count, 10);
            for (var i = 0; i < maxChunks; i++)
                Write($"{label} groundingChunks[{i}]: {response.GroundingChunkUrls[i]}");
            if (response.GroundingChunkUrls.Count > maxChunks)
                Write($"{label} groundingChunks: ... and {response.GroundingChunkUrls.Count - maxChunks} more");
        }

        if (string.IsNullOrWhiteSpace(response.SearchEntryPoint) == false)
            WriteResponseSnippet($"{label} searchEntryPoint", response.SearchEntryPoint, maxLength: 2000);
    }
}
