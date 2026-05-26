#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseIntercomMessageSelectStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work)
    {
        if (!ChatSlashParseTokens.IsIntercomTopicDelegate(work.Head)
            || !string.Equals(work.Action, "message", StringComparison.OrdinalIgnoreCase)
            || !SlashParseIntercomMessageArgs.TryParseSelectVerb(
                work.TopicArgsBeforeNormalize,
                work.Args,
                out var subAction,
                out var messageArgs))
        {
            return null;
        }

        return SlashParseIntercomMessageFindStage.namespaceResult(work, subAction, messageArgs);
    }
}
