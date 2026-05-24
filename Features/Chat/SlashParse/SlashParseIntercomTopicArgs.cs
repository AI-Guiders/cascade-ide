#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal static class SlashParseIntercomTopicArgs
{
    public static string Normalize(string action, string args)
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
}
