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
    /// <summary>Колонка 0 MainGrid: ширина региона Pfd в px (0 — свернуть).</summary>
    public static void ApplyPfdRegionColumnWidth(Grid mainGrid, double widthPixels)
    {
        if (mainGrid.ColumnDefinitions.Count <= 0)
            return;
        var w = Math.Max(0, widthPixels);
        mainGrid.ColumnDefinitions[0].Width = new GridLength(w);
    }

    /// <summary>Регион Pfd: видимая ширина по умолчанию или 0.</summary>
    public static void ApplyPfdRegionExpanded(Grid mainGrid, bool visible) =>
        ApplyPfdRegionColumnWidth(
            mainGrid,
            visible ? UiWorkspaceLayoutRuntimeMetrics.PfdRegionDefaultWidthPixels : 0);

    /// <summary>Колонки 3–4 MainGrid и нижней строки Workspace Health: регион Mfd и сплиттер перед ним.</summary>
    public static void ApplyMfdRegionColumns(Grid mainGrid, Grid? workspaceHealthColumnsGrid, double mfdRegionWidthPixels)
    {
        var w = Math.Max(0, mfdRegionWidthPixels);
        var splitter = w > 0 ? UiWorkspaceLayoutRuntimeMetrics.MainGridColumnSplitterWidthPixels : 0;

        if (mainGrid.ColumnDefinitions.Count > 4)
        {
            mainGrid.ColumnDefinitions[3].Width = new GridLength(splitter);
            mainGrid.ColumnDefinitions[4].Width = new GridLength(w);
        }

        if (workspaceHealthColumnsGrid is { ColumnDefinitions.Count: > 4 } inner)
        {
            inner.ColumnDefinitions[3].Width = new GridLength(splitter);
            inner.ColumnDefinitions[4].Width = new GridLength(w);
        }
    }

    /// <summary>Найти MainGrid и WorkspaceHealthColumnsGrid по корню окна и применить ширину региона Mfd.</summary>
    public static bool TryApplyMfdRegionColumnsFromRoot(Visual root, double mfdRegionWidthPixels)
    {
        if (UiControlAppearance.FindControlByName(root, "MainGrid") is not Grid main || main.ColumnDefinitions.Count <= 4)
            return false;
        var inner = UiControlAppearance.FindControlByName(root, "WorkspaceHealthColumnsGrid") as Grid;
        ApplyMfdRegionColumns(main, inner, mfdRegionWidthPixels);
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
