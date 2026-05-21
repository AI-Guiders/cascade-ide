using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaIntercomNavigationChromeTests
{
    [Fact]
    public void ResolveNavigationHeight_DetailMode_IncludesSpineAndTabs()
    {
        var h = SkiaIntercomNavigationChrome.ResolveNavigationHeight(forwardHost: true, overviewMode: false, topicCount: 2);
        Assert.Equal(SkiaIntercomNavigationChrome.SpineRowHeight + SkiaIntercomNavigationChrome.TabBarHeight, h);
    }

    [Fact]
    public void ResolveNavigationHeight_OverviewMode_OnlySpine()
    {
        var h = SkiaIntercomNavigationChrome.ResolveNavigationHeight(forwardHost: true, overviewMode: true, topicCount: 2);
        Assert.Equal(SkiaIntercomNavigationChrome.SpineRowHeight, h);
    }

    [Fact]
    public void DrawTopicTabBar_Overflow_ReportsHiddenCount()
    {
        var overview = Enumerable.Range(0, 10)
            .Select(i => new ChatThreadOverviewItem(
                Guid.NewGuid(),
                $"Topic{i}",
                "",
                IsActive: i == 0,
                IsMainThread: i == 0,
                Depth: 0,
                ItemCount: 1))
            .ToList();

        using var bitmap = new SKBitmap(800, 80);
        using var canvas = new SKCanvas(bitmap);
        var layout = SkiaIntercomNavigationChrome.DrawTopicTabBar(
            canvas,
            800,
            0,
            SkiaChatTheme.DarkFallback,
            overview,
            overview[0].ThreadId,
            SettingsDefaultsLoader.CreateDefault().Fonts.Intercom,
            maxVisibleTabs: 5);

        Assert.True(layout.OverflowHiddenCount > 0);
        Assert.NotEmpty(layout.TabHits);
        Assert.True(layout.CreateButtonBounds.Width > 0);
    }
}
