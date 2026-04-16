using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using CascadeIDE.Features.UiChrome;
using MainDockCols = CascadeIDE.Features.UiChrome.UiWorkspaceLayoutDimensions.MainWindowMainGridColumns;
using EditorDockCols = CascadeIDE.Features.UiChrome.UiWorkspaceLayoutDimensions.EditorContentGridColumns;

namespace CascadeIDE.Services;

/// <summary>
/// Применение размеров панелей главного окна: одна точка для VM, code-behind и <c>ide_set_panel_size</c>.
/// </summary>
public static class UiWorkspaceLayout
{
    /// <summary>Колонка PFD MainGrid: ширина региона Pfd в px (0 — свернуть).</summary>
    public static void ApplyPfdRegionColumnWidth(Grid mainGrid, double widthPixels)
    {
        if (mainGrid.ColumnDefinitions.Count <= MainDockCols.PfdRegion)
            return;
        var w = Math.Max(0, widthPixels);
        mainGrid.ColumnDefinitions[MainDockCols.PfdRegion].Width = new GridLength(w);
    }

    /// <summary>Регион Pfd: видимая ширина по умолчанию или 0.</summary>
    public static void ApplyPfdRegionExpanded(Grid mainGrid, bool visible) =>
        ApplyPfdRegionColumnWidth(
            mainGrid,
            visible ? UiWorkspaceLayoutRuntimeMetrics.PfdRegionDefaultWidthPixels : 0);

    /// <summary>Колонка MFD в MainGrid (не Forward): ширина региона Mfd (px); при 0 — колонка схлопнута.</summary>
    public static void ApplyMfdRegionColumns(Grid mainGrid, Grid? workspaceHealthColumnsGrid, double mfdRegionWidthPixels)
    {
        var w = Math.Max(0, mfdRegionWidthPixels);

        if (mainGrid.ColumnDefinitions.Count >= MainDockCols.Count)
            mainGrid.ColumnDefinitions[MainDockCols.MfdRegion].Width = new GridLength(w);

        // Дублирующая сетка (если есть в разметке) с тем же порядком колонок, что у MainGrid.
        if (workspaceHealthColumnsGrid is { ColumnDefinitions.Count: >= MainDockCols.Count } inner)
            inner.ColumnDefinitions[MainDockCols.MfdRegion].Width = new GridLength(w);
    }

    /// <summary>Найти MainGrid и WorkspaceHealthColumnsGrid по корню окна и применить ширину региона Mfd.</summary>
    public static bool TryApplyMfdRegionColumnsFromRoot(Visual root, double mfdRegionWidthPixels)
    {
        if (UiControlAppearance.FindControlByName(root, "MainGrid") is not Grid main
            || main.ColumnDefinitions.Count < MainDockCols.Count)
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
        if (editorContentGrid.ColumnDefinitions.Count <= EditorDockCols.MarkdownPreview)
            return;
        var previewCol = EditorDockCols.MarkdownPreview;
        editorContentGrid.ColumnDefinitions[previewCol].Width = showPreview
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0, GridUnitType.Pixel);
    }
}
