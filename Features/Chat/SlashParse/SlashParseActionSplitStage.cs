#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseActionSplitStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work)
    {
        var actionSplit = work.Tail.IndexOf(' ');
        work.Action = actionSplit < 0 ? work.Tail : work.Tail[..actionSplit];
        work.Args = actionSplit < 0 ? "" : work.Tail[(actionSplit + 1)..].Trim();
        if (!ChatSlashParseTokens.IsToken(work.Action))
            return ChatSlashCommandParseResult.Reject($"Некорректный action «{work.Action}».");

        SlashParseAnchorPeekGlue.TryApply(work);
        work.TopicArgsBeforeNormalize = work.Args;

        if (ChatSlashParseTokens.IsIntercomTopicDelegate(work.Head))
            work.Args = SlashParseIntercomTopicArgs.Normalize(work.Action, work.Args);

        if (ChatSlashParseTokens.IsIntercomTopicDelegate(work.Head))
            work.Args = SlashParseIntercomMessageArgs.Normalize(work.Action, work.Args);

        return null;
    }
}
