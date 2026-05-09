using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Сборка строк из совпадений <see cref="RipgrepWorkspaceMatch"/> после поиска.</summary>
[ComputingUnit(note: "command palette goto ripgrep rows")]
public static class CommandPaletteGoToRipgrepNavRowsProjection
{
    private const int PreviewMaxChars = 160;

    public static IEnumerable<CommandPaletteGoToNavRowPresentation> FromMatches(
        IReadOnlyList<RipgrepWorkspaceMatch> matches,
        string workspaceRoot,
        GoToAllQuery query)
    {
        var prefix = $"{query.Prefix}:";
        foreach (var m in matches)
        {
            var rel = CommandPaletteGoToWorkspacePresentation.TryRelativePath(workspaceRoot, m.Path);
            var preview = m.LineText.Trim();
            if (preview.Length > PreviewMaxChars)
                preview = preview[..(PreviewMaxChars - 1)] + "…";

            var subtitle = rel is not null
                ? $"{rel} · {m.LineNumber}"
                : $"{m.Path} · {m.LineNumber}";

            yield return new CommandPaletteGoToNavRowPresentation(
                Title: preview,
                SubtitleCategory: subtitle,
                FullPath: m.Path,
                Line: m.LineNumber,
                Column: 1,
                PrefixHint: prefix);
        }
    }
}
