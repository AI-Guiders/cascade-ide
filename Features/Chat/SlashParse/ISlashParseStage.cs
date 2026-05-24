#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal interface ISlashParseStage
{
    /// <returns><see langword="null"/> — передать следующей стадии.</returns>
    ChatSlashCommandParseResult? TryApply(SlashParseWork work);
}
