using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaIntercomTopicNavigatorHitTests
{
    [Fact]
    public void MapRowBoundsToPanel_matches_listTop_minus_scroll()
    {
        const float left = 0f;
        const float top = 72f;
        const float height = 400f;
        const float scrollOffset = 12f;

        var panel = SkiaIntercomTopicNavigator.ComputePanelLayout(left, top, height, rowCount: 3);
        var rowInListSpace = new SKRect(left + SkiaIntercomTopicNavigator.Pad, 0f, left + SkiaIntercomTopicNavigator.PanelWidth - SkiaIntercomTopicNavigator.Pad, SkiaIntercomTopicNavigator.RowHeight);

        var mapped = SkiaIntercomTopicNavigator.MapRowBoundsToPanel(rowInListSpace, panel, scrollOffset);

        Assert.Equal(panel.ListTop - scrollOffset, mapped.Top, precision: 3);
        Assert.Equal(panel.SearchBounds.Bottom + SkiaIntercomTopicNavigator.Pad - scrollOffset, mapped.Top, precision: 3);
    }

    [Fact]
    public void ComputePanelLayout_listTop_is_below_search_field()
    {
        const float top = 40f;
        var panel = SkiaIntercomTopicNavigator.ComputePanelLayout(0f, top, 300f, rowCount: 1);

        Assert.Equal(top + SkiaIntercomTopicNavigator.Pad + SkiaIntercomTopicNavigator.SearchHeight + SkiaIntercomTopicNavigator.Pad, panel.ListTop, precision: 3);
        Assert.Equal(panel.SearchBounds.Bottom + SkiaIntercomTopicNavigator.Pad, panel.ListTop, precision: 3);
    }
}
