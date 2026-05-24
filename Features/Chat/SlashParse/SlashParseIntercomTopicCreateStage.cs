#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseIntercomTopicCreateStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work)
    {
        if (!ChatSlashParseTokens.IsIntercomTopicDelegate(work.Head)
            || !string.Equals(work.Action, "topic", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(work.TopicArgsBeforeNormalize, "create", StringComparison.OrdinalIgnoreCase))
        {
            return buildResult("");
        }

        const string createPrefix = "create ";
        if (work.TopicArgsBeforeNormalize.StartsWith(createPrefix, StringComparison.OrdinalIgnoreCase))
            return buildResult(work.Args);

        return null;
    }

    private static ChatSlashCommandParseResult buildResult(string titleArgs) =>
        new(
            true,
            false,
            ChatSlashCommandShape.NamespaceAction,
            "intercom",
            "topic",
            "create",
            titleArgs,
            null);
}
