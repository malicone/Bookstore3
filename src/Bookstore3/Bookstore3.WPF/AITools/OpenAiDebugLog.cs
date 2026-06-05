namespace Bookstore3.WPF.AITools;

internal sealed class OpenAiDebugLog : AiFileDebugLog
{
    public static OpenAiDebugLog Instance { get; } = new();

    private OpenAiDebugLog()
    {
    }

    protected override string LogFileName => "openai-ai-debug.log";

    protected override string LogPrefix => "OpenAI";
}
