#nullable enable

namespace CascadeIDE.Features.Chat;

public enum ChatSlashCommandShape
{
    Flat,
    NamespaceAction,
}

public readonly record struct ChatSlashCommandParseResult(
    bool IsSlashLine,
    bool IsRejected,
    ChatSlashCommandShape Shape,
    string Head,
    string? Action,
    string ArgsTail,
    string? RejectReason)
{
    public static ChatSlashCommandParseResult NotSlash() =>
        new(false, false, ChatSlashCommandShape.Flat, "", null, "", null);

    public static ChatSlashCommandParseResult Reject(string reason) =>
        new(true, true, ChatSlashCommandShape.Flat, "", null, "", reason);
}

/// <summary>Разбор строки <c>/verb</c> или <c>/namespace action …</c> (ADR 0119).</summary>
public static class ChatSlashCommandParser
{
    public static ChatSlashCommandParseResult TryParse(string? raw)
    {
        var line = raw?.Trim() ?? "";
        if (line.Length == 0 || line[0] != '/')
            return ChatSlashCommandParseResult.NotSlash();

        var body = line[1..].Trim();
        if (body.Length == 0)
            return ChatSlashCommandParseResult.Reject("Пустая команда после «/». Введи /help.");

        var spaceIdx = body.IndexOf(' ');
        var headToken = spaceIdx < 0 ? body : body[..spaceIdx];
        var tail = spaceIdx < 0 ? "" : body[(spaceIdx + 1)..].Trim();

        if (!TrySplitHead(headToken, out var head, out var inlineAction))
            return ChatSlashCommandParseResult.Reject($"Некорректный verb «{headToken}».");

        if (inlineAction is null && !string.IsNullOrEmpty(tail) && IsFlatVerbWithArgTail(head))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.Flat,
                head,
                null,
                tail,
                null);
        }

        if (inlineAction is not null)
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.NamespaceAction,
                head,
                inlineAction,
                tail,
                null);
        }

        if (string.IsNullOrEmpty(tail))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.Flat,
                head,
                null,
                "",
                null);
        }

        var actionSplit = tail.IndexOf(' ');
        var action = actionSplit < 0 ? tail : tail[..actionSplit];
        var args = actionSplit < 0 ? "" : tail[(actionSplit + 1)..].Trim();
        if (!IsToken(action))
            return ChatSlashCommandParseResult.Reject($"Некорректный action «{action}».");

        return new ChatSlashCommandParseResult(
            true,
            false,
            ChatSlashCommandShape.NamespaceAction,
            head,
            action,
            args,
            null);
    }

    private static bool TrySplitHead(string headToken, out string head, out string? inlineAction)
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

    private static bool IsFlatVerbWithArgTail(string head) =>
        string.Equals(head, "card", StringComparison.OrdinalIgnoreCase)
        || string.Equals(head, "spine", StringComparison.OrdinalIgnoreCase);

    private static bool IsToken(string token)
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
}
