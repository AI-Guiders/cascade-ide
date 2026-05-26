#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Канонические slash-пути и нормализация args (alias table).</summary>
internal static class SlashPathAliases
{
    public const string AnchorPeekPath = "/anchor peek";

    private const string IntercomAnchorPeekBody = "intercom anchor peek";
    private const string AnchorPeekBody = "anchor peek";

    public static bool IsAnchorPeekCommand(in ChatSlashCommandParseResult parse) =>
        string.Equals(parse.Head, "anchor", StringComparison.OrdinalIgnoreCase)
            && parse.Action?.StartsWith("peek", StringComparison.OrdinalIgnoreCase) == true
        || string.Equals(parse.Head, "intercom", StringComparison.OrdinalIgnoreCase)
            && string.Equals(parse.Action, "anchor", StringComparison.OrdinalIgnoreCase)
            && parse.ArgsTail.TrimStart().StartsWith("peek", StringComparison.OrdinalIgnoreCase);

    public static bool TryGetCanonical(in ChatSlashCommandParseResult parse, out string canonicalPath, out string argsTail)
    {
        canonicalPath = "";
        argsTail = "";
        if (!IsAnchorPeekCommand(parse))
            return false;

        canonicalPath = AnchorPeekPath;
        argsTail = ExtractPeekArgs(parse) ?? "";
        return true;
    }

    public static string? ExtractPeekArgs(in ChatSlashCommandParseResult parse)
    {
        if (!IsAnchorPeekCommand(parse))
            return null;

        if (string.Equals(parse.Head, "intercom", StringComparison.OrdinalIgnoreCase))
        {
            var args = parse.ArgsTail.Trim();
            if (args.StartsWith("peek ", StringComparison.OrdinalIgnoreCase))
                return args[5..];

            return args.Length > 4 ? args[4..] : "";
        }

        if (string.Equals(parse.Action, "peek", StringComparison.OrdinalIgnoreCase))
            return parse.ArgsTail;

        return parse.Action!.Length > 4 ? parse.Action[4..] + parse.ArgsTail : parse.ArgsTail;
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
