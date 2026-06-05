namespace Bookstore3.WPF.AITools;

internal interface IAiDebugLog
{
    string FilePath { get; }

    void BeginSession(string summary);

    void Write(string message);

    void WriteResponseSnippet(string label, string? text, int maxLength = 8000);

    void WriteException(string label, Exception ex);
}
