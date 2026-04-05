using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Изменить размер панели по имени (ширину колонки или высоту строки). Для ide_set_panel_size.
/// Реализация — <see cref="UiWorkspaceLayout"/>.
/// </summary>
public static class UiPanelResize
{
    /// <summary>Панели: solution_explorer, chat — width (px); build_output, terminal — height (px).</summary>
    public static string Resize(Visual root, string panel, double? width, double? height)
    {
        var mainGrid = UiControlAppearance.FindControlByName(root, "MainGrid") as Grid;
        if (mainGrid is null)
            return "MainGrid not found.";

        switch (panel.Trim().ToLowerInvariant())
        {
            case "solution_explorer":
                if (width is not { } w0)
                    return "solution_explorer requires width (pixels).";
                if (mainGrid.ColumnDefinitions.Count <= 0) return "Invalid grid.";
                UiWorkspaceLayout.ApplySolutionExplorerColumnWidth(mainGrid, w0);
                return "OK";
            case "chat":
                if (width is not { } w4)
                    return "chat requires width (pixels).";
                if (!UiWorkspaceLayout.TryApplyChatPanelColumnsFromRoot(root, w4))
                    return "Invalid grid.";
                return "OK";
            case "terminal":
                if (height is not { } h)
                    return "terminal requires height (pixels).";
                if (mainGrid.RowDefinitions.Count <= 4) return "Invalid grid.";
                UiWorkspaceLayout.ApplyBottomSplitterRowHeight(mainGrid, h);
                return "OK";
            case "build_output":
                if (height is not { } bh)
                    return "build_output requires height (pixels).";
                var editorGrid = UiControlAppearance.FindControlByName(root, "EditorColumnGrid") as Grid;
                if (editorGrid is null || editorGrid.RowDefinitions.Count <= 3)
                    return "EditorColumnGrid not found or invalid.";
                UiWorkspaceLayout.ApplyBuildOutputRowHeight(editorGrid, bh);
                return "OK";
            default:
                return $"Unknown panel: {panel}. Use: solution_explorer, chat, build_output, terminal.";
        }
    }
}
