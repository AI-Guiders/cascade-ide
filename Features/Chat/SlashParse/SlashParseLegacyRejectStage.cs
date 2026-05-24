#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseLegacyRejectStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work) =>
        SlashParseIntercomLegacy.TryRejectRemovedHead(work.Head, out var reason)
            ? ChatSlashCommandParseResult.Reject(reason)
            : null;
}
