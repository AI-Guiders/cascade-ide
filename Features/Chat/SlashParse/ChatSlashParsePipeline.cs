#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal static class ChatSlashParsePipeline
{
    private static readonly ISlashParseStage[] Stages =
    [
        new SlashParseBootstrapStage(),
        new SlashParseLegacyRejectStage(),
        new SlashParseEarlyShapeStage(),
        new SlashParseActionSplitStage(),
        new SlashParseIntercomTopicCreateStage(),
        new SlashParseIntercomMessageFindStage(),
        new SlashParseIntercomMessageRelateStage(),
        new SlashParseEditorLineStage(),
        new SlashParseSolutionNewStage(),
        new SlashParseTopicInspectStage(),
        new SlashParseIntercomMessageSelectStage(),
        new SlashParseDefaultNamespaceStage(),
    ];

    public static ChatSlashCommandParseResult Parse(string? raw)
    {
        var work = new SlashParseWork { RawLine = raw ?? "" };
        foreach (var stage in Stages)
        {
            var result = stage.TryApply(work);
            if (result is not null)
                return result.Value;
        }

        throw new InvalidOperationException("Slash parse pipeline ended without result.");
    }
}
