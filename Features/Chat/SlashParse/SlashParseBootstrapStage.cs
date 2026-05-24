#nullable enable

namespace CascadeIDE.Features.Chat.SlashParse;

internal sealed class SlashParseBootstrapStage : ISlashParseStage
{
    public ChatSlashCommandParseResult? TryApply(SlashParseWork work)
    {
        var line = work.RawLine.Trim();
        if (line.Length == 0 || line[0] != '/')
            return ChatSlashCommandParseResult.NotSlash();

        work.Body = line[1..].Trim();
        if (work.Body.Length == 0)
            return ChatSlashCommandParseResult.Reject("Пустая команда после «/». Введи /help.");

        var spaceIdx = work.Body.IndexOf(' ');
        work.HeadToken = spaceIdx < 0 ? work.Body : work.Body[..spaceIdx];
        work.Tail = spaceIdx < 0 ? "" : work.Body[(spaceIdx + 1)..].Trim();

        if (!ChatSlashParseTokens.TrySplitHead(work.HeadToken, out var head, out var inlineAction))
            return ChatSlashCommandParseResult.Reject($"Некорректный verb «{work.HeadToken}».");

        work.Head = head;
        work.InlineAction = inlineAction;
        return null;
    }
}
