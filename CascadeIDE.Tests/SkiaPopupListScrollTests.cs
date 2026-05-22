using CascadeIDE.Views.SkiaKit;
using SkiaSharp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaPopupListScrollTests
{
    [Fact]
    public void EnsureSelectionVisible_scrolls_down_when_selection_below_viewport()
    {
        const int rows = 12;
        var scroll = SkiaPopupList.EnsureSelectionVisible(0, 0, rows);
        Assert.Equal(0, scroll);

        scroll = SkiaPopupList.EnsureSelectionVisible(8, scroll, rows);
        Assert.Equal(3, scroll);
        Assert.Equal(8, scroll + SkiaPopupList.ViewportRowCount(rows) - 1);
    }

    [Fact]
    public void EnsureSelectionVisible_scrolls_up_when_selection_above_viewport()
    {
        const int rows = 12;
        var scroll = SkiaPopupList.EnsureSelectionVisible(10, 0, rows);
        Assert.Equal(5, scroll);

        scroll = SkiaPopupList.EnsureSelectionVisible(2, scroll, rows);
        Assert.Equal(2, scroll);
    }

    [Fact]
    public void HitTestRow_respects_scroll_offset()
    {
        var bounds = new SKRect(0, 0, 200, SkiaPopupList.MeasureHeight(12));
        const int rows = 12;
        const int scroll = 4;

        var firstVisible = SkiaPopupList.HitTestRow(
            bounds,
            10,
            bounds.Top + SkiaPopupList.HorizontalPadding + 2,
            rows,
            scroll);
        Assert.Equal(4, firstVisible);

        var secondVisible = SkiaPopupList.HitTestRow(
            bounds,
            10,
            bounds.Top + SkiaPopupList.HorizontalPadding + SkiaPopupList.RowHeight + 2,
            rows,
            scroll);
        Assert.Equal(5, secondVisible);
    }

    [Fact]
    public void MeasureHeight_caps_at_viewport()
    {
        Assert.Equal(
            SkiaPopupList.ViewportRowCount(3) * SkiaPopupList.RowHeight + SkiaPopupList.HorizontalPadding * 2,
            SkiaPopupList.MeasureHeight(3));
        Assert.Equal(
            SkiaPopupList.ViewportRowCount(20) * SkiaPopupList.RowHeight + SkiaPopupList.HorizontalPadding * 2,
            SkiaPopupList.MeasureHeight(20));
    }

    [Fact]
    public void MeasureHeight_includes_hierarchy_header_when_requested()
    {
        var without = SkiaPopupList.MeasureHeight(2, showHierarchyHeader: false);
        var withHeader = SkiaPopupList.MeasureHeight(2, showHierarchyHeader: true);
        Assert.Equal(without + SkiaPopupList.HierarchyHeaderHeight, withHeader);
    }
}
