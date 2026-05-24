#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseIntercomMessageRelateStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work)
    {
        if (!ChatSlashParseTokens.IsIntercomTopicDelegate(work.Head)
            || !string.Equals(work.Action, "message", StringComparison.OrdinalIgnoreCase)
            || !SlashParseIntercomMessageArgs.TryParseRelatePrefix(work.TopicArgsBeforeNormalize, out var relateTail))
        {
            return null;
        }

        return SlashParseIntercomMessageFindStage.namespaceResult(work, "relate", relateTail);
    }
}
