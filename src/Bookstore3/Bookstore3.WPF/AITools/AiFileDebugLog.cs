using System.IO;

namespace Bookstore3.WPF.AITools;

internal abstract class AiFileDebugLog : IAiDebugLog
{
    private readonly object _sync = new();

    protected abstract string LogFileName { get; }

    protected abstract string LogPrefix { get; }

    public string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Bookstore3",
        LogFileName);

    public void BeginSession(string summary)
    {
        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.AppendAllText(
                FilePath,
                $"{Environment.NewLine}========== {DateTime.Now:yyyy-MM-dd HH:mm:ss} =========={Environment.NewLine}" +
                $"Log file: {FilePath}{Environment.NewLine}" +
                $"{summary}{Environment.NewLine}");
        }
    }

    public void Write(string message) =>
        AppendLine(message);

    public void WriteResponseSnippet(string label, string? text, int maxLength = 8000)
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

    public void WriteException(string label, Exception ex)
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

    protected void AppendLine(string message, bool includeTimestamp = true)
    {
        var line = includeTimestamp
            ? $"[{LogPrefix}] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}"
            : message;

        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.AppendAllText(FilePath, line + Environment.NewLine);
        }
    }
}
