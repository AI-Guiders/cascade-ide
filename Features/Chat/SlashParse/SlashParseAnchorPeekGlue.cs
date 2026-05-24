#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal static class SlashParseAnchorPeekGlue
{
    public static void TryApply(SlashParseWork work)
    {
        if (!string.Equals(work.Head, "anchor", StringComparison.OrdinalIgnoreCase)
            || !work.Action.StartsWith("peek", StringComparison.OrdinalIgnoreCase)
            || work.Action.Length <= 4)
        {
            return;
        }

        work.Args = string.IsNullOrEmpty(work.Args) ? work.Action[4..] : work.Action[4..] + " " + work.Args;
        work.Action = "peek";
    }
}
