#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal static class ChatSlashParseTokens
{
    public static bool IsToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        foreach (var ch in token)
        {
            if (char.IsLetterOrDigit(ch) || ch == '-')
                continue;

            return false;
        }

        return char.IsLetter(token[0]);
    }

    public static bool TrySplitHead(string headToken, out string head, out string? inlineAction)
    {
        inlineAction = null;
        head = headToken;
        if (!IsToken(headToken))
            return false;

        var slash = headToken.IndexOf('/');
        if (slash <= 0 || slash >= headToken.Length - 1)
            return true;

        var ns = headToken[..slash];
        var act = headToken[(slash + 1)..];
        if (!IsToken(ns) || !IsToken(act))
            return false;

        head = ns;
        inlineAction = act;
        return true;
    }

    public static string FirstToken(string tail)
    {
        if (string.IsNullOrWhiteSpace(tail))
            return "";

        var space = tail.IndexOf(' ');
        return space < 0 ? tail : tail[..space];
    }

    public static string SecondToken(string tail)
    {
        if (string.IsNullOrWhiteSpace(tail))
            return "";

        var firstSpace = tail.IndexOf(' ');
        if (firstSpace < 0)
            return "";

        var rest = tail[(firstSpace + 1)..].TrimStart();
        var secondSpace = rest.IndexOf(' ');
        return secondSpace < 0 ? rest : rest[..secondSpace];
    }

    public static bool IsIntercomTopicDelegate(string head) =>
        string.Equals(head, "intercom", StringComparison.OrdinalIgnoreCase);
}
