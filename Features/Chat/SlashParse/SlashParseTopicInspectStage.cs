#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseTopicInspectStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work)
    {
        if (ChatSlashParseTokens.IsIntercomTopicDelegate(work.Head))
            return tryIntercomTopicInspect(work);

        if (!string.Equals(work.Head, "topic", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!string.Equals(work.Action, "list", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(work.Action, "tree", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(work.Args))
            return null;

        var subSplit = work.Args.IndexOf(' ');
        var first = subSplit < 0 ? work.Args : work.Args[..subSplit];
        if (!string.Equals(first, "text", StringComparison.OrdinalIgnoreCase))
            return null;

        var remainder = subSplit < 0 ? "" : work.Args[(subSplit + 1)..].Trim();
        return SlashParseIntercomMessageFindStage.namespaceResult(work, "text", remainder);
    }

    private static ChatSlashCommandParseResult? tryIntercomTopicInspect(SlashParseWork work)
    {
        if (!string.Equals(work.Action, "topic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(work.Args))
        {
            return null;
        }

        var verb = ChatSlashParseTokens.FirstToken(work.Args);
        if (!string.Equals(verb, "list", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(verb, "tree", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var afterVerb = work.Args[(verb.Length)..].TrimStart();
        if (!string.Equals(ChatSlashParseTokens.FirstToken(afterVerb), "text", StringComparison.OrdinalIgnoreCase))
            return null;

        var afterText = afterVerb[(ChatSlashParseTokens.FirstToken(afterVerb).Length)..].TrimStart();
        return SlashParseIntercomMessageFindStage.namespaceResult(work, verb + " text", afterText);
    }
}
