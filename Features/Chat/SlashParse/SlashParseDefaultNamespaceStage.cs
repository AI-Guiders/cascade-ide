#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseDefaultNamespaceStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work) =>
        new(
            true,
            false,
            ChatSlashCommandShape.NamespaceAction,
            work.Head,
            work.Action,
            null,
            work.Args,
            null);
}
