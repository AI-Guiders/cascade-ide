#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseSolutionNewStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work)
    {
        if (!string.Equals(work.Head, "solution", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(work.Action, "new", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var subSplit = work.Args.IndexOf(' ');
        var sub = subSplit < 0 ? work.Args : work.Args[..subSplit];
        if (!isSolutionNewTemplate(sub))
            return null;

        var projectArgs = subSplit < 0 ? "" : work.Args[(subSplit + 1)..].Trim();
        return SlashParseIntercomMessageFindStage.namespaceResult(work, sub, projectArgs);
    }

    private static bool isSolutionNewTemplate(string token) =>
        string.Equals(token, "console", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "classlib", StringComparison.OrdinalIgnoreCase)
        || string.Equals(token, "webapi", StringComparison.OrdinalIgnoreCase);
}
