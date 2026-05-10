#nullable enable
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Выполнение выбранной строки палитры команд (MCP или go-to).</summary>
internal static class IdeCommandPaletteExecutionOrchestrator
{
    public static Task RunSelectionAsync(
        IdeCommandPaletteRowViewModel row,
        IIdeMcpActions ideMcp,
        IdeMcpCommandExecutor ideMcpExecutor,
        CancellationToken cancellationToken = default)
    {
        if (row.RowKind == IdeCommandPaletteRowKind.Hint)
            return Task.CompletedTask;

        if (row.RowKind == IdeCommandPaletteRowKind.GoTo)
        {
            if (string.IsNullOrEmpty(row.NavigateFilePath))
                return Task.CompletedTask;
            if (row.NavigateLine > 0)
                ideMcp.GoToPosition(row.NavigateFilePath, row.NavigateLine, row.NavigateColumn, null, null);
            else
                ideMcp.OpenFile(row.NavigateFilePath);
            return Task.CompletedTask;
        }

        if (!row.IsAvailable)
            return Task.CompletedTask;

        var args = IdeCommandPaletteCatalog.ParseArgs(row.ArgsJson);
        return ideMcpExecutor.ExecuteAsync(row.CommandId, args, cancellationToken);
    }
}
