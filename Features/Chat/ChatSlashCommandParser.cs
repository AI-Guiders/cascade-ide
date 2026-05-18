#nullable enable
using CascadeIDE.Services;

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
    string? SubAction,
    string ArgsTail,
    string? RejectReason)
{
    public static ChatSlashCommandParseResult NotSlash() =>
        new(false, false, ChatSlashCommandShape.Flat, "", null, null, "", null);

    public static ChatSlashCommandParseResult Reject(string reason) =>
        new(true, true, ChatSlashCommandShape.Flat, "", null, null, "", reason);
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

        if (inlineAction is null && !string.IsNullOrEmpty(tail) && IsFlatVerbWithArgTail(head, tail))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.Flat,
                head,
                null,
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
                null,
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
                null,
                "",
                null);
        }

        var actionSplit = tail.IndexOf(' ');
        var action = actionSplit < 0 ? tail : tail[..actionSplit];
        var args = actionSplit < 0 ? "" : tail[(actionSplit + 1)..].Trim();
        if (!IsToken(action))
            return ChatSlashCommandParseResult.Reject($"Некорректный action «{action}».");

        if (TryParseEditorLineSubAction(head, action, args, out var subAction, out var lineArgs))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.NamespaceAction,
                head,
                action,
                subAction,
                lineArgs,
                null);
        }

        if (TryParseSolutionNewSubAction(head, action, args, out subAction, out var projectArgs))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.NamespaceAction,
                head,
                action,
                subAction,
                projectArgs,
                null);
        }

        if (TryParseTopicInspectTextSubAction(head, action, args, out subAction, out var inspectArgs))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.NamespaceAction,
                head,
                action,
                subAction,
                inspectArgs,
                null);
        }

        return new ChatSlashCommandParseResult(
            true,
            false,
            ChatSlashCommandShape.NamespaceAction,
            head,
            action,
            null,
            args,
            null);
    }

    private static bool TryParseEditorLineSubAction(
        string head,
        string action,
        string args,
        out string subAction,
        out string lineArgs)
    {
        subAction = "";
        lineArgs = args;

        if (!string.Equals(head, "editor", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(action, "line", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var subSplit = args.IndexOf(' ');
        var sub = subSplit < 0 ? args : args[..subSplit];
        if (!IsEditorLineSubAction(sub))
            return false;

        subAction = sub;
        lineArgs = subSplit < 0 ? "" : args[(subSplit + 1)..].Trim();
        return true;
    }

    private static bool IsEditorLineSubAction(string token) =>
        string.Equals(token, "select", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "delete", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseSolutionNewSubAction(
        string head,
        string action,
        string args,
        out string subAction,
        out string projectArgs)
    {
        subAction = "";
        projectArgs = args;

        if (!string.Equals(head, "solution", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(action, "new", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var subSplit = args.IndexOf(' ');
        var sub = subSplit < 0 ? args : args[..subSplit];
        if (!IsSolutionNewTemplate(sub))
            return false;

        subAction = sub;
        projectArgs = subSplit < 0 ? "" : args[(subSplit + 1)..].Trim();
        return true;
    }

    private static bool IsSolutionNewTemplate(string token) =>
        string.Equals(token, "console", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "classlib", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "webapi", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseTopicInspectTextSubAction(
        string head,
        string action,
        string args,
        out string subAction,
        out string remainder)
    {
        subAction = "";
        remainder = args;

        if (!string.Equals(head, "topic", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(action, "list", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(action, "tree", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(args))
            return false;

        var subSplit = args.IndexOf(' ');
        var first = subSplit < 0 ? args : args[..subSplit];
        if (!string.Equals(first, "text", StringComparison.OrdinalIgnoreCase))
            return false;

        subAction = "text";
        remainder = subSplit < 0 ? "" : args[(subSplit + 1)..].Trim();
        return true;
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

    private static bool IsFlatVerbWithArgTail(string head, string tail)
    {
        if (string.Equals(head, "card", StringComparison.OrdinalIgnoreCase))
            return true;

        // /spine set <focus> — flat; /spine open|list|tree|show|toggle — namespace (см. intent-catalog).
        if (!string.Equals(head, "spine", StringComparison.OrdinalIgnoreCase))
            return false;

        var first = tail.IndexOf(' ') < 0 ? tail : tail[..tail.IndexOf(' ')];
        return !IsSpineNamespaceActionToken(first);
    }

    private static bool IsSpineNamespaceActionToken(string token) =>
        string.Equals(token, "set", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "show", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "toggle", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "list", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "tree", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "open", StringComparison.OrdinalIgnoreCase);

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

    /// <summary>
    /// После выбора подсказки с путём: сразу выполнить open/load без второго Enter.
    /// </summary>
    public static bool ShouldAutoExecuteAfterAutocompleteCommit(string? chatInput)
    {
        var parse = TryParse(chatInput);
        if (!parse.IsSlashLine || parse.IsRejected)
            return false;

        if (!ChatSlashCommandCatalog.TryResolve(parse, out var descriptor))
            return false;

        if (descriptor.ExecutionKind == ChatSlashCommandExecutionKind.LocalIntercom
            && descriptor.SlashPath is "/topic open" or "/topic cards" or "/spine open")
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(parse.ArgsTail))
            return false;

        return descriptor.CommandId is IdeCommands.OpenFile or IdeCommands.LoadSolution;
    }
}
