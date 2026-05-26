#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseEditorLineStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work)
    {
        if (!string.Equals(work.Head, "editor", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(work.Action, "line", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var subSplit = work.Args.IndexOf(' ');
        var sub = subSplit < 0 ? work.Args : work.Args[..subSplit];
        if (!isEditorLineSubAction(sub))
            return null;

        var lineArgs = subSplit < 0 ? "" : work.Args[(subSplit + 1)..].Trim();
        return SlashParseIntercomMessageFindStage.namespaceResult(work, sub, lineArgs);
    }

    private static bool isEditorLineSubAction(string token) =>
        string.Equals(token, "select", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "delete", StringComparison.OrdinalIgnoreCase);
}
