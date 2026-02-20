using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Изменить размер панели по имени (ширину колонки или высоту строки). Для ide_set_panel_size.
/// Вызывать из UI-потока.
/// </summary>
public static class UiPanelResize
{
    /// <summary>Панели: solution_explorer, chat — ширина (width); build_output, terminal — высота (height).</summary>
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
                mainGrid.ColumnDefinitions[0] = new ColumnDefinition(new GridLength(Math.Max(0, w0), GridUnitType.Pixel));
                return "OK";
            case "chat":
                if (width is not { } w4)
                    return "chat requires width (pixels).";
                if (mainGrid.ColumnDefinitions.Count <= 4) return "Invalid grid.";
                mainGrid.ColumnDefinitions[4] = new ColumnDefinition(new GridLength(Math.Max(80, w4), GridUnitType.Pixel));
                return "OK";
            case "terminal":
                if (height is not { } h)
                    return "terminal requires height (pixels).";
                if (mainGrid.RowDefinitions.Count <= 4) return "Invalid grid.";
                mainGrid.RowDefinitions[4] = new RowDefinition(new GridLength(Math.Max(80, h), GridUnitType.Pixel));
                return "OK";
            case "build_output":
                if (height is not { } bh)
                    return "build_output requires height (pixels).";
                var editorGrid = UiControlAppearance.FindControlByName(root, "EditorColumnGrid") as Grid;
                if (editorGrid is null || editorGrid.RowDefinitions.Count <= 3)
                    return "EditorColumnGrid not found or invalid.";
                editorGrid.RowDefinitions[3] = new RowDefinition(new GridLength(Math.Max(80, bh), GridUnitType.Pixel));
                return "OK";
            default:
                return $"Unknown panel: {panel}. Use: solution_explorer, chat, build_output, terminal.";
        }
    }
}
