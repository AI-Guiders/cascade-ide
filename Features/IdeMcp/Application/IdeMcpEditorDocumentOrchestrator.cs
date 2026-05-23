#nullable enable
using CascadeIDE.Contracts;
using CascadeIDE.Features.Launch.DataAcquisition;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>UI-мутации редактора/документов для MCP (маршалинг остаётся на хосте).</summary>
[ApplicationOrchestrator]
public static class IdeMcpEditorDocumentOrchestrator
{
    public static void ScheduleOpenFile(
        IUiScheduler scheduler,
        string path,
        Action<string> openNormalizedExistingFile)
    {
        if (string.IsNullOrEmpty(path))
            return;

        var pathCopy = path;
        scheduler.Post(() =>
        {
            var normalized = LaunchProjectPathResolver.NormalizeExistingProjectFileFullPath(pathCopy);
            if (normalized is null)
                return;
            openNormalizedExistingFile(normalized);
        });
    }

    public static void ScheduleLoadSolution(IUiScheduler scheduler, string path, Action<string> loadSolution)
    {
        if (string.IsNullOrEmpty(path))
            return;

        var pathCopy = path;
        scheduler.Post(() => loadSolution(pathCopy));
    }
}
