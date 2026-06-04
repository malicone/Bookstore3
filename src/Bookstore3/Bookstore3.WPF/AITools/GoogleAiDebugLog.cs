using System.IO;

namespace Bookstore3.WPF.AITools;

internal static class GoogleAiDebugLog
{
    private const string LogFileName = "google-ai-debug.log";
    private static readonly object Sync = new();
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Bookstore3",
        LogFileName);

    public static string FilePath => LogFilePath;

    public static void Write(string message) => AppendLine(message);

    public static void WriteResponseSnippet(string label, string? text, int maxLength = 8000)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Write($"{label}: (empty)");
            return;
        }

        var snippet = text.Length <= maxLength
            ? text
            : text[..maxLength] + "...";
        Write($"{label} ({text.Length} chars):");
        AppendLine(snippet, includeTimestamp: false);
    }

    public static void WriteGroundingMetadata(string label, GoogleGeminiRestResponse response)
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

    public static void WriteException(string label, Exception ex)
    {
        Write($"{label}: {ex.GetType().Name}: {ex.Message}");
        var inner = ex.InnerException;
        while (inner is not null)
        {
            Write($"{label}: inner {inner.GetType().Name}: {inner.Message}");
            inner = inner.InnerException;
        }

        if (string.IsNullOrWhiteSpace(ex.StackTrace) == false)
            AppendLine(ex.StackTrace, includeTimestamp: false);
    }

    public static void BeginSession(string summary)
    {
        lock (Sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
            File.AppendAllText(
                LogFilePath,
                $"{Environment.NewLine}========== {DateTime.Now:yyyy-MM-dd HH:mm:ss} =========={Environment.NewLine}" +
                $"Log file: {LogFilePath}{Environment.NewLine}" +
                $"{summary}{Environment.NewLine}");
        }
    }

    private static void AppendLine(string message, bool includeTimestamp = true)
    {
        var line = includeTimestamp
            ? $"[GoogleAI] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}"
            : message;

        lock (Sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
    }
}
