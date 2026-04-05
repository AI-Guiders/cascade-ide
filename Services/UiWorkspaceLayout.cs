using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Services;

/// <summary>
/// Применение размеров панелей главного окна: одна точка для VM, code-behind и <c>ide_set_panel_size</c>.
/// </summary>
public static class UiWorkspaceLayout
{
    /// <summary>Колонка 0 MainGrid: ширина в px (0 — скрыть дерево).</summary>
    public static void ApplySolutionExplorerColumnWidth(Grid mainGrid, double widthPixels)
    {
        if (mainGrid.ColumnDefinitions.Count <= 0)
            return;
        var w = Math.Max(0, widthPixels);
        mainGrid.ColumnDefinitions[0].Width = new GridLength(w);
    }

    /// <summary>Дерево решения: видимая ширина по умолчанию или 0.</summary>
    public static void ApplySolutionExplorerVisible(Grid mainGrid, bool visible) =>
        ApplySolutionExplorerColumnWidth(
            mainGrid,
            visible ? UiWorkspaceLayoutRuntimeMetrics.SolutionExplorerDefaultWidthPixels : 0);

    /// <summary>Колонки 3–4 MainGrid и нижней строки телеметрии: чат и сплиттер перед ним.</summary>
    public static void ApplyChatPanelColumns(Grid mainGrid, Grid? workspaceTelemetryColumnsGrid, double chatWidthPixels)
    {
        var w = Math.Max(0, chatWidthPixels);
        var splitter = w > 0 ? UiWorkspaceLayoutRuntimeMetrics.MainGridColumnSplitterWidthPixels : 0;

        if (mainGrid.ColumnDefinitions.Count > 4)
        {
            mainGrid.ColumnDefinitions[3].Width = new GridLength(splitter);
            mainGrid.ColumnDefinitions[4].Width = new GridLength(w);
        }

        if (workspaceTelemetryColumnsGrid is { ColumnDefinitions.Count: > 4 } inner)
        {
            inner.ColumnDefinitions[3].Width = new GridLength(splitter);
            inner.ColumnDefinitions[4].Width = new GridLength(w);
        }
    }

    /// <summary>Найти MainGrid и WorkspaceTelemetryColumnsGrid по корню окна и применить ширину чата.</summary>
    public static bool TryApplyChatPanelColumnsFromRoot(Visual root, double chatWidthPixels)
    {
        if (UiControlAppearance.FindControlByName(root, "MainGrid") is not Grid main || main.ColumnDefinitions.Count <= 4)
            return false;
        var inner = UiControlAppearance.FindControlByName(root, "WorkspaceTelemetryColumnsGrid") as Grid;
        ApplyChatPanelColumns(main, inner, chatWidthPixels);
        return true;
    }

    /// <summary>Строка 4 MainGrid — высота зоны над нижней панелью (терминал / сборка / …).</summary>
    public static void ApplyBottomSplitterRowHeight(Grid mainGrid, double heightPixels)
    {
        if (mainGrid.RowDefinitions.Count <= 4)
            return;
        var h = Math.Max(UiWorkspaceLayoutRuntimeMetrics.BottomPanelMinRowPixels, heightPixels);
        mainGrid.RowDefinitions[4].Height = new GridLength(h);
    }

    /// <summary>Строка вывода сборки в <c>EditorColumnGrid</c>.</summary>
    public static void ApplyBuildOutputRowHeight(Grid editorColumnGrid, double heightPixels)
    {
        if (editorColumnGrid.RowDefinitions.Count <= 3)
            return;
        var h = Math.Max(UiWorkspaceLayoutRuntimeMetrics.BottomPanelMinRowPixels, heightPixels);
        editorColumnGrid.RowDefinitions[3].Height = new GridLength(h);
    }

    /// <summary>Вторая колонка <c>EditorContentGrid</c>: превью Markdown рядом с редактором.</summary>
    public static void ApplyMarkdownPreviewColumn(Grid editorContentGrid, bool showPreview)
    {
        if (editorContentGrid.ColumnDefinitions.Count <= 1)
            return;
        editorContentGrid.ColumnDefinitions[1].Width = showPreview
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0, GridUnitType.Pixel);
    }
}
