using CascadeIDE.Contracts;
using CascadeIDE.Features.Workspace.Application;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Фильтрация файлов решения для префикса <c>f:</c> через <see cref="WorkspaceFileIndex"/>.</summary>
[PresentationProjection("command palette goto file rows")]
public static class CommandPaletteGoToFileNavRowsProjection
{
    public static IEnumerable<CommandPaletteGoToNavRowPresentation> EnumerateFiltered(
        WorkspaceFileIndex index,
        string filterTermTrimmedWhenNonEmptyOrEmptyMeansAll,
        string workspaceRoot,
        int maxFiles)
    {
        var matches = index.Search(filterTermTrimmedWhenNonEmptyOrEmptyMeansAll, maxFiles);
        foreach (var m in matches)
        {
            var rel = CommandPaletteGoToWorkspacePresentation.TryRelativePath(workspaceRoot, m.FullPath);
            yield return new CommandPaletteGoToNavRowPresentation(
                Title: m.Title,
                SubtitleCategory: rel ?? m.InsertPath,
                FullPath: m.FullPath,
                Line: 0,
                Column: 1,
                PrefixHint: "f:");
        }
    }
}
