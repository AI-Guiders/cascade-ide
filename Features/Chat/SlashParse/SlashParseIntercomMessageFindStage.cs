#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseIntercomMessageFindStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work)
    {
        if (!ChatSlashParseTokens.IsIntercomTopicDelegate(work.Head)
            || !string.Equals(work.Action, "message", StringComparison.OrdinalIgnoreCase)
            || !SlashParseIntercomMessageArgs.TryParseFindPrefix(work.TopicArgsBeforeNormalize, out var findTail))
        {
            return null;
        }

        return namespaceResult(work, "find", findTail);
    }

    internal static ChatSlashCommandParseResult namespaceResult(SlashParseWork work, string subAction, string args) =>
        new(
            true,
            false,
            ChatSlashCommandShape.NamespaceAction,
            work.Head,
            work.Action,
            subAction,
            args,
            null);
}
