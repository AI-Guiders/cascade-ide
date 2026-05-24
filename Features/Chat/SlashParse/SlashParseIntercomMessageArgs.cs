#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal static class SlashParseIntercomMessageArgs
{
    public static string Normalize(string action, string args)
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

    public static bool TryParseFindPrefix(string args, out string findTail)
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

    public static bool TryParseRelatePrefix(string args, out string relateTail)
    {
        relateTail = "";
        const string marker = " relate ";
        var idx = args.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return false;

        relateTail = args[..idx].Trim() + " " + args[(idx + marker.Length)..].Trim();
        return relateTail.Length > 0 && relateTail.Contains(' ', StringComparison.Ordinal);
    }

    public static bool TryParseSelectVerb(
        string topicArgsBeforeNormalize,
        string normalizedArgs,
        out string subAction,
        out string messageArgs)
    {
        subAction = "";
        messageArgs = normalizedArgs;
        foreach (var verb in new[] { "select", "next", "prev" })
        {
            if (string.Equals(topicArgsBeforeNormalize, verb, StringComparison.OrdinalIgnoreCase)
                || topicArgsBeforeNormalize.StartsWith(verb + " ", StringComparison.OrdinalIgnoreCase))
            {
                subAction = verb;
                messageArgs = normalizedArgs;
                return true;
            }
        }

        return false;
    }
}
