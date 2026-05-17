#nullable enable
using CascadeIDE.Contracts;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>UI-мутации редактора/документов для MCP (маршалинг остаётся на хосте).</summary>
[ApplicationOrchestrator]
public static class IdeMcpEditorDocumentActions
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
            if (!File.Exists(pathCopy))
                return;
            openNormalizedExistingFile(CanonicalFilePath.Normalize(pathCopy));
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
