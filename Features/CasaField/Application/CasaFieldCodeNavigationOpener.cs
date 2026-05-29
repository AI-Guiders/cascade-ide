#nullable enable

using CascadeIDE.Features.CasaField.Application;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.CasaField.Application;

/// <summary>Open code target from CASA field query (code_anchors in claims_nav).</summary>
public static class CasaFieldCodeNavigationOpener
{
    public static bool TryOpenCodeTarget(MainWindowViewModel vm, CasaFieldTarget target)
    {
        if (target.Kind != "code" || string.IsNullOrWhiteSpace(target.CodeFile))
            return false;

        var ws = vm.GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws))
            return false;

        var fullPath = Path.IsPathRooted(target.CodeFile)
            ? target.CodeFile
            : Path.GetFullPath(Path.Combine(ws, target.CodeFile.Replace('/', Path.DirectorySeparatorChar)));

        if (!File.Exists(fullPath))
            return false;

        var line = target.CodeLine ?? 1;
        vm.IdeMcp.GoToPosition(fullPath, line, 1);
        return true;
    }
}
