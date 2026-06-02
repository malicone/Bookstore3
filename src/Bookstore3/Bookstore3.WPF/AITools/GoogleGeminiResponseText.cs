using System.Text;
using Mscc.GenerativeAI.Types;

namespace Bookstore3.WPF.AITools;

internal static class GoogleGeminiResponseText
{
    public static string GetTextOrThrow(GenerateContentResponse? response, string contextLabel = "Google AI")
    {
        var text = TryGetText(response);
        if (string.IsNullOrWhiteSpace(text) == false)
            return text;

        throw new InvalidOperationException(BuildEmptyContentMessage(response, contextLabel));
    }

    public static string? TryGetText(GenerateContentResponse? response)
    {
        if (response is null)
            return null;

        if (string.IsNullOrWhiteSpace(response.Text) == false)
            return response.Text;

        var fromParts = ExtractFromCandidateParts(response, includeThoughtParts: false);
        if (string.IsNullOrWhiteSpace(fromParts) == false)
            return fromParts;

        fromParts = ExtractFromCandidateParts(response, includeThoughtParts: true);
        if (string.IsNullOrWhiteSpace(fromParts) == false)
            return fromParts;

        if (string.IsNullOrWhiteSpace(response.Thinking) == false)
            return response.Thinking;

        return null;
    }

    private static string? ExtractFromCandidateParts(GenerateContentResponse response, bool includeThoughtParts)
    {
        var candidates = response.Candidates;
        if (candidates is null || candidates.Count == 0)
            return null;

        var sb = new StringBuilder();
        foreach (var candidate in candidates)
        {
            var parts = candidate.Content?.Parts;
            if (parts is null)
                continue;

            foreach (var part in parts)
            {
                if (includeThoughtParts == false && part.Thought == true)
                    continue;

                if (string.IsNullOrWhiteSpace(part.Text))
                    continue;

                if (sb.Length > 0)
                    sb.AppendLine();

                sb.Append(part.Text);
            }
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }

    private static string BuildEmptyContentMessage(GenerateContentResponse? response, string contextLabel)
    {
        var details = new List<string>();

        var feedback = response?.PromptFeedback;
        if (feedback?.BlockReason is not null)
            details.Add($"prompt blocked ({feedback.BlockReason})");

        if (string.IsNullOrWhiteSpace(feedback?.BlockReasonMessage) == false)
            details.Add(feedback.BlockReasonMessage);

        var candidate = response?.Candidates?.FirstOrDefault();
        if (candidate?.FinishReason is not null)
            details.Add($"finish reason: {candidate.FinishReason}");

        if (details.Count == 0)
            return $"{contextLabel} returned empty content.";

        return $"{contextLabel} returned empty content ({string.Join("; ", details)}).";
    }
}
