using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Фильтрация файлов решения для префикса <c>f:</c>.</summary>
[PresentationProjection("command palette goto file rows")]
public static class CommandPaletteGoToFileNavRowsProjection
{
    public static IEnumerable<CommandPaletteGoToNavRowPresentation> EnumerateFiltered(
        IEnumerable<(string Title, string FullPath)> files,
        string filterTermTrimmedWhenNonEmptyOrEmptyMeansAll,
        string workspaceRoot,
        int maxFiles)
    {
        IEnumerable<(string Title, string FullPath)> query = files;
        if (!string.IsNullOrWhiteSpace(filterTermTrimmedWhenNonEmptyOrEmptyMeansAll))
        {
            var t = filterTermTrimmedWhenNonEmptyOrEmptyMeansAll.Trim();
            query = files.Where(e =>
                e.Title.Contains(t, StringComparison.OrdinalIgnoreCase)
                || e.FullPath.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var (title, path) in query.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase).Take(maxFiles))
        {
            var rel = CommandPaletteGoToWorkspacePresentation.TryRelativePath(workspaceRoot, path);
            yield return new CommandPaletteGoToNavRowPresentation(
                Title: title,
                SubtitleCategory: rel ?? path,
                FullPath: path,
                Line: 0,
                Column: 1,
                PrefixHint: "f:");
        }
    }
}
