#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Канонические slash-пути и нормализация args (alias table).</summary>
internal static class SlashPathAliases
{
    public const string AnchorPeekPath = "/anchor peek";

    private const string IntercomAnchorPeekPath = "/intercom anchor peek";
    private const string IntercomAnchorPeekBody = "intercom anchor peek";
    private const string AnchorPeekBody = "anchor peek";

    public static bool IsAnchorPeekPath(string canonicalPath) =>
        string.Equals(canonicalPath, AnchorPeekPath, StringComparison.OrdinalIgnoreCase)
        || string.Equals(canonicalPath, IntercomAnchorPeekPath, StringComparison.OrdinalIgnoreCase);

    public static string? ExtractPeekArgs(string canonicalPath, string? argTail)
    {
        if (!IsAnchorPeekPath(canonicalPath))
            return null;

        return string.IsNullOrWhiteSpace(argTail) ? "" : argTail.Trim();
    }

    public static string NormalizeCompletionBody(string body)
    {
        if (body.Equals(IntercomAnchorPeekBody, StringComparison.OrdinalIgnoreCase))
            return AnchorPeekBody;

        var prefix = IntercomAnchorPeekBody + " ";
        if (body.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return AnchorPeekBody + " " + body[prefix.Length..];

        return body;
    }

    public static bool IsAnchorPeekCompletionBody(string body)
    {
        var normalized = NormalizeCompletionBody(body);
        return normalized.Equals(AnchorPeekBody, StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith(AnchorPeekBody + " ", StringComparison.OrdinalIgnoreCase);
    }
}
