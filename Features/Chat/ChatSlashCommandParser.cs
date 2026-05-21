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

        if (TryRejectRemovedIntercomLegacyHead(head, out var legacyReason))
            return ChatSlashCommandParseResult.Reject(legacyReason);

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

        var topicArgsBeforeNormalize = args;
        if (IsIntercomTopicDelegate(head))
            args = NormalizeIntercomTopicCommandArgs(action, args);

        if (IsIntercomTopicDelegate(head))
            args = NormalizeIntercomMessageCommandArgs(action, args);

        if (IsIntercomTopicDelegate(head)
            && string.Equals(action, "topic", StringComparison.OrdinalIgnoreCase)
            && string.Equals(topicArgsBeforeNormalize, "create", StringComparison.OrdinalIgnoreCase))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.NamespaceAction,
                head,
                action,
                "create",
                "",
                null);
        }

        if (IsIntercomTopicDelegate(head)
            && string.Equals(action, "message", StringComparison.OrdinalIgnoreCase)
            && isIntercomMessageFindPrefix(topicArgsBeforeNormalize, out var findTail))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.NamespaceAction,
                head,
                action,
                "find",
                findTail,
                null);
        }

        if (IsIntercomTopicDelegate(head)
            && string.Equals(action, "message", StringComparison.OrdinalIgnoreCase)
            && isIntercomMessageRelatePrefix(topicArgsBeforeNormalize, out var relateTail))
        {
            return new ChatSlashCommandParseResult(
                true,
                false,
                ChatSlashCommandShape.NamespaceAction,
                head,
                action,
                "relate",
                relateTail,
                null);
        }

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

        if (IsIntercomTopicDelegate(head))
        {
            if (!string.Equals(action, "topic", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(args))
            {
                return false;
            }

            var verb = firstToken(args);
            if (!string.Equals(verb, "list", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(verb, "tree", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var afterVerb = args[(verb.Length)..].TrimStart();
            if (!string.Equals(firstToken(afterVerb), "text", StringComparison.OrdinalIgnoreCase))
                return false;

            subAction = verb + " text";
            var afterText = afterVerb[(firstToken(afterVerb).Length)..].TrimStart();
            remainder = afterText;
            return true;
        }

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

    private static bool IsIntercomTopicDelegate(string head) =>
        string.Equals(head, "intercom", StringComparison.OrdinalIgnoreCase);

    /// <summary>Устаревшие top-level слэши Intercom (ADR 0136) — только <c>/intercom …</c>.</summary>
    private static bool TryRejectRemovedIntercomLegacyHead(string head, out string reason)
    {
        reason = "";
        if (string.Equals(head, "topic", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom topic … (например /intercom topic list, /intercom topic open <id>).";
            return true;
        }

        if (string.Equals(head, "spine", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom spine … (например /intercom spine show, /intercom spine set <фокус>).";
            return true;
        }

        if (string.Equals(head, "overview", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom overview или /intercom topic cards.";
            return true;
        }

        if (string.Equals(head, "attach", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom attach … (selection, scope, file).";
            return true;
        }

        if (string.Equals(head, "card", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom topic create <название>.";
            return true;
        }

        if (string.Equals(head, "thread", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Команда перенесена: /intercom message select|next|prev.";
            return true;
        }

        return false;
    }

    private static string NormalizeIntercomTopicCommandArgs(string action, string args)
    {
        if (!string.Equals(action, "topic", StringComparison.OrdinalIgnoreCase))
            return args;

        if (string.Equals(args, "create", StringComparison.OrdinalIgnoreCase))
            return "";

        const string createPrefix = "create ";
        if (args.StartsWith(createPrefix, StringComparison.OrdinalIgnoreCase))
            return args[createPrefix.Length..].Trim();

        return args;
    }

    private static bool isIntercomMessageFindPrefix(string args, out string findTail)
    {
        findTail = "";
        if (string.Equals(args, "find", StringComparison.OrdinalIgnoreCase))
            return true;

        const string prefix = "find ";
        if (args.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            findTail = args[prefix.Length..].Trim();
            return true;
        }

        return false;
    }

    private static bool isIntercomMessageRelatePrefix(string args, out string relateTail)
    {
        relateTail = "";
        const string marker = " relate ";
        var idx = args.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return false;

        relateTail = args[..idx].Trim() + " " + args[(idx + marker.Length)..].Trim();
        return relateTail.Length > 0 && relateTail.Contains(' ', StringComparison.Ordinal);
    }

    private static string NormalizeIntercomMessageCommandArgs(string action, string args)
    {
        if (!string.Equals(action, "message", StringComparison.OrdinalIgnoreCase))
            return args;

        foreach (var verb in new[] { "select", "next", "prev" })
        {
            if (string.Equals(args, verb, StringComparison.OrdinalIgnoreCase))
                return "";

            var prefix = verb + " ";
            if (args.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return args[prefix.Length..].Trim();
        }

        return args;
    }

    private static bool IsFlatVerbWithArgTail(string head, string tail)
    {
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

    private static string firstToken(string tail)
    {
        if (string.IsNullOrWhiteSpace(tail))
            return "";
        var space = tail.IndexOf(' ');
        return space < 0 ? tail : tail[..space];
    }

    private static string secondToken(string tail)
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
            && descriptor.SlashPath is "/intercom topic open" or "/intercom topic cards" or "/intercom spine open")
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(parse.ArgsTail))
            return false;

        return descriptor.CommandId is IdeCommands.OpenFile or IdeCommands.LoadSolution;
    }
}
