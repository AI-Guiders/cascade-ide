#nullable enable
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views;

public partial class SkiaChatSurfaceControl
{
    public override void Render(DrawingContext context)
    {
        RefreshTheme();

        var snapshot = Snapshot ?? ChatSurfaceSnapshot.Empty;
        var width = Math.Max(160, Bounds.Width);
        var topicCountForMeasure = snapshot.Layout.Overview.Count;
        var showNavigatorForMeasure = ShouldShowTopicNavigator(topicCountForMeasure);
        var navigatorPadForMeasure = showNavigatorForMeasure ? SkiaIntercomTopicNavigator.PanelWidth : 0f;
        var showFeedGutterForMeasure = !OverviewMode
            && (snapshot.Layout.Lanes.Any(l => l.Entries.Any(e => e.Kind == ChatSurfaceEntryKind.Message)));
        var gutterPadForMeasure = showFeedGutterForMeasure ? SkiaChatDrawContext.FeedGutterWidth : 0f;
        var contentWidth = (float)(width - 24 - gutterPadForMeasure - navigatorPadForMeasure);
        var maxChars = Math.Max(18, (int)(contentWidth / 7.1f));
        var measureContext = new SkiaChatMeasureContext(maxChars, contentWidth);
        var layoutCacheKey = BuildFeedLayoutCacheKey(snapshot);
        var chromeOnly = _chromeOnlyInvalidation;
        _chromeOnlyInvalidation = false;

        IReadOnlyList<SkiaChatPlacedEntity> placed;
        if (chromeOnly
            && layoutCacheKey == _feedLayoutCacheKey
            && _feedLayoutCache is not null)
        {
            placed = _feedLayoutCache;
        }
        else
        {
            var entities = SkiaChatSceneBuilder.Build(
                snapshot,
                OverviewMode,
                DetailThreadId,
                FeedUsesForwardMetrics,
                IntercomFonts,
                CompactSideHost);
            placed = SkiaChatLayoutEngine.Layout(entities, measureContext);
            _feedLayoutCache = placed;
            _feedLayoutCacheKey = layoutCacheKey;
        }

        _cachedContentHeight = SkiaChatLayoutEngine.TotalHeight(placed);

        var topicCount = snapshot.Layout.Overview.Count;
        var showOverviewCatalog = OverviewMode && topicCount > 0;
        var showTopicTabBar = ForwardHost && !OverviewMode && topicCount > 0;
        var statusSubtitle = ForwardHost
            ? MergeStatusSubtitles(
                ChatIntercomChromeStatusPresentation.FormatSubtitle(snapshot, OverviewMode, DetailThreadId, showTopicTabBar),
                FmUsageSubtitle)
            : null;
        var bottomChrome = (float)ResolveBottomChromeHeight((float)Math.Max(160, Bounds.Width));
        ClampScrollToContent(showOverviewCatalog, statusSubtitle, bottomChrome);

        var layoutScale = ResolveLayoutScale();
        var logicalHeight = Math.Max(1, Bounds.Height);
        var pixelWidth = Math.Max(1, (int)Math.Ceiling(width * layoutScale));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(logicalHeight * layoutScale));
        var surfaceColor = _theme.Surface;
        var fallbackBrush = new SolidColorBrush(
            Color.FromArgb(byte.MaxValue, surfaceColor.Red, surfaceColor.Green, surfaceColor.Blue));
        var destRect = new Rect(Bounds.Size);
        context.FillRectangle(fallbackBrush, destRect);

        if (!IsIntercomSkiaRenderingEnabled())
            return;

        var bitmap = EnsureSkiaFrameBitmap(pixelWidth, pixelHeight, layoutScale);
        using (var framebuffer = bitmap.Lock())
        {
            var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var skSurface = SKSurface.Create(info, framebuffer.Address, framebuffer.RowBytes);
            if (skSurface is null)
                return;

            var canvas = skSurface.Canvas;
            canvas.Save();
            canvas.Scale(layoutScale);
            DrawSkiaScene(
                canvas,
                (float)width,
                (float)logicalHeight,
                placed,
                (float)_scrollOffset,
                showOverviewCatalog,
                snapshot.Layout.Overview.Count,
                statusSubtitle,
                bottomChrome,
                layoutScale);
            canvas.Restore();
        }

        var srcRect = new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height);
        context.DrawImage(bitmap, srcRect, destRect);
    }

    private string BuildFeedLayoutCacheKey(ChatSurfaceSnapshot snapshot) =>
        $"{snapshot.State.ActiveThreadId:N}|{OverviewMode}|{DetailThreadId:N}|{snapshot.Layout.Lanes.Count}|{snapshot.Layout.Overview.Count}|{SelectedMessageIndex}|{ComfortableFeed}|{ForwardHost}";

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new SkiaComposerAutomationPeer(this);

    private static bool IsIntercomSkiaRenderingEnabled() =>
        !string.Equals(
            Environment.GetEnvironmentVariable("CASCADE_INTERCOM_SKIA"),
            "0",
            StringComparison.Ordinal);

    private static bool HasScopeStrip(ChatSurfaceSnapshot? snapshot) =>
        snapshot is not null
        && (snapshot.SedmScopeStrip.HasContent || snapshot.SedmScopeStrip.OpenWorklineCount > 1);

    private float ResolveLayoutScale()
    {
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        return scale > 0 ? (float)scale : 1f;
    }

    private WriteableBitmap EnsureSkiaFrameBitmap(int pixelWidth, int pixelHeight, float layoutScale)
    {
        if (_skiaFrame is not null
            && _skiaFrameWidth == pixelWidth
            && _skiaFrameHeight == pixelHeight
            && Math.Abs(_skiaFrameLayoutScale - layoutScale) < 0.001f)
            return _skiaFrame;

        _skiaFrame?.Dispose();
        _skiaFrameWidth = pixelWidth;
        _skiaFrameHeight = pixelHeight;
        _skiaFrameLayoutScale = layoutScale;
        var dpi = 96.0 * layoutScale;
        _skiaFrame = new WriteableBitmap(
            new PixelSize(pixelWidth, pixelHeight),
            new Vector(dpi, dpi),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        return _skiaFrame;
    }

    /// <summary>
    /// Рисует сцену в локальный SKSurface (не lease canvas TopLevel — иначе затирается всё окно, Avalonia #5932).
    /// </summary>
    private void DrawSkiaScene(
        SKCanvas canvas,
        float width,
        float height,
        IReadOnlyList<SkiaChatPlacedEntity> placed,
        float scrollOffset,
        bool showOverviewCatalog,
        int overviewTopicCount,
        string? statusSubtitle,
        float bottomChrome,
        float layoutScale)
    {
        canvas.Clear(_theme.Surface);
        _chatHits.Clear();

        var topicCount = Snapshot?.Layout.Overview.Count ?? overviewTopicCount;
        var showTopicTabBar = ForwardHost && !OverviewMode && topicCount > 0;

        var chromeTop = SkiaChatChromeRenderer.ResolveTopChromeHeight(
            ForwardHost,
            showOverviewCatalog,
            !string.IsNullOrWhiteSpace(statusSubtitle),
            OverviewMode,
            topicCount,
            HasScopeStrip(Snapshot));
        SkiaIntercomNavigationChrome.LayoutResult? navLayout = null;
        var showNavigator = ShouldShowTopicNavigator(topicCount);
        if (ForwardHost)
        {
            SkiaChatChromeRenderer.Draw(
                canvas,
                width,
                _theme,
                ChromeTitle,
                OverviewMode,
                IsChatLoading,
                LoadingStatusText,
                statusSubtitle,
                showNavigatorToggle: true,
                navigatorVisible: TopicNavigatorVisible,
                out var overviewBounds,
                out var navigatorToggleBounds,
                IntercomFonts);
            registerChromePointerHits(overviewBounds, navigatorToggleBounds);

            if (topicCount > 0 && Snapshot is { } snapNav)
            {
                var toolbarH = SkiaChatChromeRenderer.ResolveToolbarHeight(
                    true,
                    !string.IsNullOrWhiteSpace(statusSubtitle));
                var navTop = toolbarH;
                navTop = SkiaIntercomNavigationChrome.DrawSpineRow(
                    canvas,
                    width,
                    navTop,
                    _theme,
                    snapNav.ProductSpine,
                    IntercomFonts);
                if (snapNav.SedmScopeStrip.HasContent || snapNav.SedmScopeStrip.OpenWorklineCount > 1)
                {
                    navTop = SkiaIntercomNavigationChrome.DrawScopeStripRow(
                        canvas,
                        width,
                        navTop,
                        _theme,
                        snapNav.SedmScopeStrip,
                        IntercomFonts);
                }
                if (!OverviewMode)
                {
                    navLayout = SkiaIntercomNavigationChrome.DrawTopicTabBar(
                        canvas,
                        width,
                        navTop,
                        _theme,
                        snapNav.Layout.Overview,
                        DetailThreadId,
                        IntercomFonts);
                    registerNavigationPointerHits(navLayout);
                }
            }
        }

        if (showOverviewCatalog)
        {
            var bandTop = ForwardHost
                ? SkiaChatChromeRenderer.ResolveToolbarHeight(true, !string.IsNullOrWhiteSpace(statusSubtitle))
                  + SkiaIntercomNavigationChrome.ResolveNavigationHeight(
                      true,
                      overviewMode: true,
                      topicCount,
                      HasScopeStrip(Snapshot))
                : 0f;
            SkiaChatChromeRenderer.DrawOverviewCatalogBand(canvas, width, bandTop, _theme, overviewTopicCount, IntercomFonts);
        }

        var showFeedGutter = !OverviewMode && (Snapshot?.Layout.Lanes.Any(l => l.Entries.Any(e => e.Kind == ChatSurfaceEntryKind.Message)) ?? false);
        var gutterPad = showFeedGutter ? SkiaChatDrawContext.FeedGutterWidth : 0f;
        var navigatorPad = showNavigator ? SkiaIntercomTopicNavigator.PanelWidth : 0f;
        const float contentLeftBase = 12f;
        var contentLeft = contentLeftBase + navigatorPad + gutterPad;
        var contentWidth = width - 24f - gutterPad - navigatorPad;
        var contentBottom = Math.Max(chromeTop + 1f, height - bottomChrome);

        if (showNavigator && Snapshot is { } snapNavPanel)
        {
            var msgCounts = ChatThreadPresentation.MessageCountsByThread(snapNavPanel);
            var navRows = ChatThreadPresentation.BuildNavigatorRows(
                snapNavPanel.State.Threads,
                msgCounts,
                TopicNavigatorSearchQuery);
            var searchCaretVisible = _navigatorSearchFocused && IsKeyboardFocusWithin && _composerCaretBlinkVisible;
            var topicNavLayout = SkiaIntercomTopicNavigator.Draw(
                canvas,
                0,
                chromeTop,
                contentBottom - chromeTop,
                _theme,
                IntercomFonts,
                navRows,
                DetailThreadId,
                TopicNavigatorSearchQuery,
                _navigatorScrollOffset,
                _navigatorSearchFocused && IsKeyboardFocusWithin,
                _navigatorSearchCaretIndex,
                searchCaretVisible);
            registerTopicNavigatorPointerHits(topicNavLayout, 0);
        }

        canvas.Save();
        canvas.ClipRect(new SKRect(navigatorPad, chromeTop, width, contentBottom), antialias: false);
        canvas.Translate(0, chromeTop - scrollOffset);

        if (placed.Count == 0)
        {
            using var emptyFont = SkiaKit.SkiaKitFonts.CreateUi(11);
            using var emptyPaint = SkiaKit.SkiaKitFonts.CreateTextPaint(_theme.EmptyHint);
            SkiaKit.SkiaKitFonts.DrawText(
                canvas,
                "Пока пусто. Задай вопрос или команду.",
                contentLeft,
                28,
                SKTextAlign.Left,
                emptyFont,
                emptyPaint,
                layoutScale);
        }
        else
        {
            var feedTop = scrollOffset - chromeTop;
            var feedBottom = feedTop + (contentBottom - chromeTop);
            const float cullPad = 240f;
            for (var i = 0; i < placed.Count; i++)
            {
                var item = placed[i];
                var itemBottom = item.Top + item.Layout.Height;
                if (itemBottom < feedTop - cullPad || item.Top > feedBottom + cullPad)
                    continue;

                var itemLeft = float.IsNaN(item.Left) ? contentLeft : item.Left;
                var itemWidth = float.IsNaN(item.Width) ? contentWidth : item.Width;
                var drawContext = new SkiaChatDrawContext
                {
                    Canvas = canvas,
                    Theme = _theme,
                    ContentLeft = itemLeft,
                    ContentWidth = itemWidth,
                    ScrollOffset = scrollOffset - chromeTop,
                    ItemIndex = i,
                    HoveredItemIndex = _hoveredItem,
                    SelectedMessageIndex = SelectedMessageIndex,
                    HighlightedMessageIndices = Snapshot?.HighlightedMessageIndices,
                    HitRegistry = _chatHits
                };

                var hit = item.Entity.CreateHit(item.Layout);
                if (hit is { } h)
                {
                    var rect = new SKRect(itemLeft, item.Top, itemLeft + itemWidth, item.Top + item.Layout.Height);
                    drawContext.RegisterHit(rect, h);
                }

                item.Entity.Draw(drawContext, item.Top, item.Layout);
            }
        }

        canvas.Restore();
        DrawIntercomBottomChrome(canvas, width, height, _theme, layoutScale);
    }
}

