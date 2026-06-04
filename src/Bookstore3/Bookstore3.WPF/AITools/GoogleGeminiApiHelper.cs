using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;

namespace Bookstore3.WPF.AITools;

internal static class GoogleGeminiApiHelper
{
    public static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(90);
    public static readonly TimeSpan SearchRequestTimeout = GoogleGeminiRestClient.SearchRequestTimeout;

    public static void ConfigureModel(GenerativeModel model, bool useGoogleSearch) =>
        model.Timeout = useGoogleSearch ? SearchRequestTimeout : DefaultRequestTimeout;

    public static async Task<GenerateContentResponse> GenerateContentAsync(
        GenerativeModel model,
        GenerateContentRequest request,
        string logLabel,
        bool useGoogleSearch,
        CancellationToken cancellationToken)
    {
        var requestTimeout = useGoogleSearch ? SearchRequestTimeout : DefaultRequestTimeout;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(requestTimeout);

        GoogleAiDebugLog.Write(
            $"{logLabel}: HTTP request starting (timeout {requestTimeout.TotalSeconds:0}s, UseGoogleSearch={useGoogleSearch})...");

        try
        {
            var response = await model.GenerateContent(request, cancellationToken: timeoutCts.Token);
            GoogleAiDebugLog.Write($"{logLabel}: HTTP request completed.");
            return response;
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && cancellationToken.IsCancellationRequested == false)
        {
            throw new InvalidOperationException(
                $"Google AI timed out after {requestTimeout.TotalSeconds:0} seconds ({logLabel}).",
                ex);
        }
        catch (GeminiApiException ex)
        {
            var detail = FormatGeminiApiError(ex);
            GoogleAiDebugLog.Write($"{logLabel}: GeminiApiException — {detail}");
            throw new InvalidOperationException($"Google AI API error ({logLabel}): {detail}", ex);
        }
        catch (Exception ex)
        {
            GoogleAiDebugLog.WriteException($"{logLabel}: unhandled", ex);
            throw;
        }
    }

    public static string FormatGeminiApiError(GeminiApiException ex)
    {
        var parts = new List<string> { ex.Message };

        if (ex.Response is not null)
        {
            parts.Add($"HTTP {(int)ex.Response.StatusCode} {ex.Response.ReasonPhrase}");
            try
            {
                var body = ex.Response.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(body) == false)
                    parts.Add(body.Length <= 2000 ? body : body[..2000] + "...");
            }
            catch
            {
                // ignore body read failures
            }
        }

        if (ex.InnerException is not null)
            parts.Add($"Inner: {ex.InnerException.Message}");

        return string.Join(Environment.NewLine, parts);
    }

    public static string FormatExceptionForDialog(Exception ex)
    {
        if (ex is InvalidOperationException && ex.InnerException is GeminiApiException gemini)
            return FormatGeminiApiError(gemini);

        if (ex.InnerException is GeminiApiException innerGemini)
            return $"{ex.Message}{Environment.NewLine}{Environment.NewLine}{FormatGeminiApiError(innerGemini)}";

        var message = ex.Message;
        if (ex.InnerException is not null)
            message += $"{Environment.NewLine}{Environment.NewLine}Details: {ex.InnerException.Message}";

        return message;
    }
}
