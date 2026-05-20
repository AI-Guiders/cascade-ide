#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Текстовый preview для параметрических slash-команд (ADR 0138, фаза A).</summary>
internal static class CockpitCommandLinePreviewBuilder
{
    public static bool TryBuild(string? bufferText, out string? summary)
    {
        summary = null;
        var parse = ChatSlashCommandParser.TryParse(bufferText);
        if (!parse.IsSlashLine || parse.IsRejected)
            return false;

        var tail = parse.ArgsTail?.Trim() ?? "";
        if (tail.Length == 0)
            return false;

        if (IsMessageSelect(parse) && ParametricSegmentListParser.TryParse(tail, out var segments, out _))
        {
            summary = ParametricSegmentListParser.FormatSummary(segments, "Сообщения");
            return true;
        }

        if (IsEditorLineSelect(parse) && ParametricSegmentListParser.TryParse(tail, out segments, out _))
        {
            summary = ParametricSegmentListParser.FormatSummary(segments, "Строки");
            return true;
        }

        return false;
    }

    private static bool IsMessageSelect(in ChatSlashCommandParseResult parse) =>
        string.Equals(parse.Head, "intercom", StringComparison.OrdinalIgnoreCase)
        && string.Equals(parse.Action, "message", StringComparison.OrdinalIgnoreCase)
        && (string.Equals(parse.SubAction, "select", StringComparison.OrdinalIgnoreCase)
            || isIntercomMessageSelectArgsTail(parse.ArgsTail));

    private static bool IsEditorLineSelect(in ChatSlashCommandParseResult parse) =>
        string.Equals(parse.Head, "editor", StringComparison.OrdinalIgnoreCase)
        && string.Equals(parse.Action, "line", StringComparison.OrdinalIgnoreCase)
        && string.Equals(parse.SubAction, "select", StringComparison.OrdinalIgnoreCase);

    private static bool isIntercomMessageSelectArgsTail(string? argsTail)
    {
        var tail = argsTail?.Trim() ?? "";
        return tail.Length > 0
               && (ParametricSegmentListParser.TryParse(tail, out _, out _)
                   || ChatSlashParametricArgsBuilder.TryParseLineRangeTail(tail, out _, out _, out _));
    }
}
