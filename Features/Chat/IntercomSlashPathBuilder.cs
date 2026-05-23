#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Построение канонического slash-пути для <c>/intercom …</c> (ADR 0136).</summary>
internal static class IntercomSlashPathBuilder
{
    public static bool TryBuildPath(ChatSlashCommandParseResult parse, out string slashPath)
    {
        slashPath = "";
        if (!parse.IsSlashLine || parse.IsRejected)
            return false;

        if (!string.Equals(parse.Head, "intercom", StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(parse.Action))
        {
            var flatTail = parse.ArgsTail.Trim();
            if (string.IsNullOrEmpty(flatTail))
            {
                slashPath = "/intercom";
                return true;
            }

            var group = firstToken(flatTail);
            var verb = secondToken(flatTail);
            if (isKnownInnerVerb(group, verb))
            {
                slashPath = $"/intercom {group} {verb}";
                return true;
            }

            slashPath = "/intercom " + flatTail;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(parse.SubAction))
        {
            if (string.Equals(parse.Action, "message", StringComparison.OrdinalIgnoreCase)
                && string.Equals(parse.SubAction, "select", StringComparison.OrdinalIgnoreCase)
                && string.Equals(parse.ArgsTail.Trim(), "clear", StringComparison.OrdinalIgnoreCase))
            {
                slashPath = "/intercom message select clear";
                return true;
            }

            slashPath = string.Equals(parse.SubAction, "list text", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(parse.SubAction, "tree text", StringComparison.OrdinalIgnoreCase)
                ? $"/intercom {parse.Action} {parse.SubAction}"
                : $"/intercom {parse.Action} {parse.SubAction}";
            return true;
        }

        if (string.IsNullOrWhiteSpace(parse.ArgsTail))
        {
            slashPath = $"/intercom {parse.Action}";
            return true;
        }

        if (string.Equals(parse.Action, "message", StringComparison.OrdinalIgnoreCase)
            && string.Equals(parse.SubAction, "find", StringComparison.OrdinalIgnoreCase))
        {
            slashPath = "/intercom message find";
            return true;
        }

        if (string.Equals(parse.Action, "message", StringComparison.OrdinalIgnoreCase)
            && string.Equals(parse.SubAction, "relate", StringComparison.OrdinalIgnoreCase))
        {
            slashPath = "/intercom message relate";
            return true;
        }

        if (string.Equals(parse.Action, "message", StringComparison.OrdinalIgnoreCase)
            && isMessageAnchorsListTail(parse.ArgsTail))
        {
            slashPath = "/intercom message anchors list";
            return true;
        }

        var tail = parse.ArgsTail.Trim();
        var innerVerb = firstToken(tail);
        if (string.Equals(parse.Action, "message", StringComparison.OrdinalIgnoreCase)
            && tail.Length > 0
            && isMessageSelectRangeTail(tail))
        {
            slashPath = "/intercom message select";
            return true;
        }

        if (isKnownInnerVerb(parse.Action, innerVerb))
        {
            slashPath = $"/intercom {parse.Action} {innerVerb}";
            return true;
        }

        if (string.Equals(parse.Action, "message", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrEmpty(tail))
        {
            slashPath = "/intercom message select";
            return true;
        }

        if (string.Equals(parse.Action, "spine", StringComparison.OrdinalIgnoreCase))
        {
            slashPath = "/intercom spine set";
            return true;
        }

        slashPath = $"/intercom {parse.Action} {tail}";
        return true;
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

    private static bool isMessageAnchorsListTail(string? argsTail) =>
        string.Equals((argsTail ?? "").Trim(), "anchors list", StringComparison.OrdinalIgnoreCase);

    private static bool isMessageSelectRangeTail(string tail)
    {
        if (ParametricSegmentListParser.TryParse(tail, out _, out _))
            return true;

        if (ChatSlashParametricArgsBuilder.TryParseLineRangeTail(tail, out _, out _, out _))
            return true;

        // Невалидный, но явно параметрический хвост — всё равно /intercom message select (preview покажет ошибку).
        if (tail.Contains('['))
            return true;

        return tail.Any(static ch => ch is >= '0' and <= '9');
    }

    private static bool isKnownInnerVerb(string group, string verb) =>
        group.ToLowerInvariant() switch
        {
            "topic" => verb is "list" or "tree" or "create" or "rename" or "open" or "cards" or "next" or "prev",
            "spine" => verb is "list" or "tree" or "set" or "show" or "toggle" or "open",
            "message" => verb is "select" or "find" or "relate" or "next" or "prev",
            "attach" => verb is "selection" or "scope" or "file",
            _ => false,
        };
}
