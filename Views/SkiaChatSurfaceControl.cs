#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CascadeIDE.Features.Chat;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;

namespace CascadeIDE.Views;

/// <summary>
/// Skia-центричный chat surface: overview веток, секции тредов и карточки сообщений/уточнений.
/// </summary>
public partial class SkiaChatSurfaceControl : Control
{
    private const double WheelPixelsPerDelta = 48;

    private readonly List<(Rect Bounds, SkiaChatHit Hit)> _hitTargets = [];
    private double _scrollOffset;
    private double _cachedContentHeight;
    private int _hoveredItem = -1;
    private SkiaChatTheme _theme = SkiaChatTheme.DarkFallback;
    private WriteableBitmap? _skiaFrame;
    private int _skiaFrameWidth;
    private int _skiaFrameHeight;

    public static readonly StyledProperty<ChatSurfaceSnapshot> SnapshotProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, ChatSurfaceSnapshot>(nameof(Snapshot), ChatSurfaceSnapshot.Empty);

    public static readonly StyledProperty<int> SelectedMessageIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(SelectedMessageIndex), -1);

    public static readonly StyledProperty<Guid> DetailThreadIdProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, Guid>(nameof(DetailThreadId), Guid.Empty);

    public static readonly StyledProperty<bool> OverviewModeProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(OverviewMode), false);

    public static readonly StyledProperty<bool> CompactLayoutProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(CompactLayout), false);

    public static readonly StyledProperty<string> ChromeTitleProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(nameof(ChromeTitle), "Intercom");

    public static readonly StyledProperty<string> LoadingStatusTextProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(nameof(LoadingStatusText), "");

    public static readonly StyledProperty<bool> IsChatLoadingProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(IsChatLoading), false);

    private SKRect _overviewButtonBounds;

    public ChatSurfaceSnapshot Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public int SelectedMessageIndex
    {
        get => GetValue(SelectedMessageIndexProperty);
        set => SetValue(SelectedMessageIndexProperty, value);
    }

    public Guid DetailThreadId
    {
        get => GetValue(DetailThreadIdProperty);
        set => SetValue(DetailThreadIdProperty, value);
    }

    public bool OverviewMode
    {
        get => GetValue(OverviewModeProperty);
        set => SetValue(OverviewModeProperty, value);
    }

    public bool CompactLayout
    {
        get => GetValue(CompactLayoutProperty);
        set => SetValue(CompactLayoutProperty, value);
    }

    public string ChromeTitle
    {
        get => GetValue(ChromeTitleProperty);
        set => SetValue(ChromeTitleProperty, value);
    }

    public string LoadingStatusText
    {
        get => GetValue(LoadingStatusTextProperty);
        set => SetValue(LoadingStatusTextProperty, value);
    }

    public bool IsChatLoading
    {
        get => GetValue(IsChatLoadingProperty);
        set => SetValue(IsChatLoadingProperty, value);
    }

    static SkiaChatSurfaceControl()
    {
        AffectsRender<SkiaChatSurfaceControl>(
            SnapshotProperty,
            SelectedMessageIndexProperty,
            DetailThreadIdProperty,
            OverviewModeProperty,
            CompactLayoutProperty,
            ChromeTitleProperty,
            LoadingStatusTextProperty,
            IsChatLoadingProperty,
            ShowIntercomComposerProperty,
            ComposerTextProperty,
            ComposerPreeditTextProperty,
            IsComposerEnabledProperty,
            IsSlashAutocompleteVisibleProperty,
            SelectedSlashSuggestionIndexProperty,
            SlashSuggestionsProperty);
    }

    public SkiaChatSurfaceControl()
    {
        ClipToBounds = true;
        MinWidth = 160;
        MinHeight = 120;
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Bubble);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);
        AddHandler(PointerExitedEvent, OnPointerExited, RoutingStrategies.Bubble);
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Bubble);
        InitializeIntercomComposer();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
        RefreshTheme();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ActualThemeVariantChanged -= OnActualThemeVariantChanged;
        _skiaFrame?.Dispose();
        _skiaFrame = null;
        _skiaFrameWidth = 0;
        _skiaFrameHeight = 0;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SnapshotProperty
            || change.Property == OverviewModeProperty
            || change.Property == DetailThreadIdProperty)
        {
            if (change.Property == OverviewModeProperty || change.Property == DetailThreadIdProperty)
                _scrollOffset = 0;

            var next = Snapshot ?? ChatSurfaceSnapshot.Empty;
            if (DetailThreadId == Guid.Empty && next.State.ActiveThreadId != Guid.Empty)
                DetailThreadId = next.State.ActiveThreadId;
            if (DetailThreadId != Guid.Empty && !next.Layout.Lanes.Any(lane => lane.Thread.ThreadId == DetailThreadId))
                DetailThreadId = next.State.ActiveThreadId;
            ClampScrollToContent();
            InvalidateVisual();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ClampScrollToContent();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsInfinity(availableSize.Width) ? MinWidth : availableSize.Width;
        var h = double.IsInfinity(availableSize.Height) ? MinHeight : availableSize.Height;
        return new Size(Math.Max(MinWidth, w), Math.Max(MinHeight, h));
    }

    protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);

    public override void Render(DrawingContext context)
    {
        RefreshTheme();

        var snapshot = Snapshot ?? ChatSurfaceSnapshot.Empty;
        var entities = SkiaChatSceneBuilder.Build(snapshot, OverviewMode, DetailThreadId, CompactLayout);
        var width = Math.Max(160, Bounds.Width);
        var contentWidth = (float)(width - 24);
        var maxChars = Math.Max(18, (int)(contentWidth / 7.1f));
        var measureContext = new SkiaChatMeasureContext(maxChars, contentWidth);
        var placed = SkiaChatLayoutEngine.Layout(entities, measureContext);
        _cachedContentHeight = SkiaChatLayoutEngine.TotalHeight(placed);

        var showOverviewCatalog = OverviewMode && snapshot.Layout.Overview.Count > 0;
        var statusSubtitle = CompactLayout
            ? ChatIntercomChromeStatusPresentation.FormatSubtitle(snapshot, OverviewMode, DetailThreadId)
            : null;
        var bottomChrome = (float)ResolveBottomChromeHeight((float)Math.Max(160, Bounds.Width));
        ClampScrollToContent(showOverviewCatalog, statusSubtitle, bottomChrome);

        var pixelWidth = Math.Max(1, (int)Math.Ceiling(width));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(Bounds.Height));
        var surfaceColor = _theme.Surface;
        var fallbackBrush = new SolidColorBrush(
            Color.FromArgb(byte.MaxValue, surfaceColor.Red, surfaceColor.Green, surfaceColor.Blue));
        var destRect = new Rect(Bounds.Size);
        context.FillRectangle(fallbackBrush, destRect);

        if (!IsIntercomSkiaRenderingEnabled())
            return;

        var bitmap = EnsureSkiaFrameBitmap(pixelWidth, pixelHeight);
        using (var framebuffer = bitmap.Lock())
        {
            var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var skSurface = SKSurface.Create(info, framebuffer.Address, framebuffer.RowBytes);
            if (skSurface is null)
                return;

            DrawSkiaScene(
                skSurface.Canvas,
                (float)pixelWidth,
                (float)pixelHeight,
                placed,
                (float)_scrollOffset,
                showOverviewCatalog,
                snapshot.Layout.Overview.Count,
                statusSubtitle,
                bottomChrome);
        }

        var srcRect = new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height);
        context.DrawImage(bitmap, srcRect, destRect);
    }

    private static bool IsIntercomSkiaRenderingEnabled() =>
        !string.Equals(
            Environment.GetEnvironmentVariable("CASCADE_INTERCOM_SKIA"),
            "0",
            StringComparison.Ordinal);

    private WriteableBitmap EnsureSkiaFrameBitmap(int width, int height)
    {
        if (_skiaFrame is not null && _skiaFrameWidth == width && _skiaFrameHeight == height)
            return _skiaFrame;

        _skiaFrame?.Dispose();
        _skiaFrameWidth = width;
        _skiaFrameHeight = height;
        _skiaFrame = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
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
        float bottomChrome)
    {
        canvas.Clear(_theme.Surface);

        var chromeTop = SkiaChatChromeRenderer.ResolveTopChromeHeight(
            CompactLayout,
            showOverviewCatalog,
            !string.IsNullOrWhiteSpace(statusSubtitle));
        if (CompactLayout)
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
                out var overviewBounds);
            _overviewButtonBounds = overviewBounds;
        }
        else
            _overviewButtonBounds = default;

        if (showOverviewCatalog)
        {
            var bandTop = CompactLayout
                ? SkiaChatChromeRenderer.ResolveToolbarHeight(true, !string.IsNullOrWhiteSpace(statusSubtitle))
                : 0f;
            SkiaChatChromeRenderer.DrawOverviewCatalogBand(canvas, width, bandTop, _theme, overviewTopicCount);
        }

        const float contentLeft = 12f;
        var contentWidth = width - 24f;
        var contentBottom = Math.Max(chromeTop + 1f, height - bottomChrome);

        canvas.Save();
        canvas.ClipRect(new SKRect(0, chromeTop, width, contentBottom), antialias: false);
        canvas.Translate(0, chromeTop - scrollOffset);

        _hitTargets.Clear();

        if (placed.Count == 0)
        {
            using var emptyFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 11);
            using var emptyPaint = new SKPaint { IsAntialias = true, Color = _theme.EmptyHint };
            canvas.DrawText("Пока пусто. Задай вопрос или команду.", contentLeft, 28, SKTextAlign.Left, emptyFont, emptyPaint);
        }
        else
        {
            for (var i = 0; i < placed.Count; i++)
            {
                var item = placed[i];
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
                    HitTargets = _hitTargets
                };
                item.Entity.Draw(drawContext, item.Top, item.Layout);

                var hit = item.Entity.CreateHit(item.Layout);
                if (hit is { } h)
                {
                    var rect = new SKRect(itemLeft, item.Top, itemLeft + itemWidth, item.Top + item.Layout.Height);
                    drawContext.RegisterHit(rect, h);
                }
            }
        }

        canvas.Restore();
        DrawIntercomBottomChrome(canvas, width, height, _theme);
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        RefreshTheme();
        InvalidateVisual();
    }

    private void RefreshTheme()
    {
        _theme = SkiaChatTheme.Resolve(this);
        var surface = _theme.Surface;
        if (surface.Alpha < byte.MaxValue)
            _theme = _theme with { Surface = surface.WithAlpha(byte.MaxValue) };
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var p = e.GetPosition(this);
        if (_composerBounds.Width > 0 && _composerBounds.Contains((float)p.X, (float)p.Y))
            return;

        _scrollOffset -= e.Delta.Y * WheelPixelsPerDelta;
        ClampScrollToContent();
        e.Handled = true;
        InvalidateVisual();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var index = FindHit(e.GetPosition(this));
        if (index == _hoveredItem)
            return;
        _hoveredItem = index;
        Cursor = _hoveredItem >= 0 ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow);
        InvalidateVisual();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (TryHandleIntercomPointer(e.GetPosition(this)))
        {
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (CompactLayout && _overviewButtonBounds.Width > 0)
        {
            var p = e.GetPosition(this);
            if (_overviewButtonBounds.Contains((float)p.X, (float)p.Y))
            {
                OverviewMode = !OverviewMode;
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        var index = FindHit(e.GetPosition(this));
        if (index < 0)
            return;
        var hit = _hitTargets[index].Hit;
        if (hit.ResetDetailMode)
            OverviewMode = true;
        else if (hit.SelectThreadId is { } threadId)
        {
            DetailThreadId = threadId;
            OverviewMode = false;
        }
        else if (hit.MessageIndex is { } messageIndex)
        {
            SelectedMessageIndex = messageIndex;
            if (hit.ToggleThinking && e.ClickCount >= 2)
                ThinkingToggleRequested?.Invoke(this, messageIndex);
        }

        e.Handled = true;
        InvalidateVisual();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (_hoveredItem < 0)
            return;
        _hoveredItem = -1;
        Cursor = new Cursor(StandardCursorType.Arrow);
        InvalidateVisual();
    }

    private int FindHit(Point point)
    {
        for (var i = 0; i < _hitTargets.Count; i++)
        {
            if (_hitTargets[i].Bounds.Contains(point))
                return i;
        }

        return -1;
    }

    private void ClampScrollToContent(bool? showOverviewCatalog = null, string? statusSubtitle = null, float? bottomChrome = null)
    {
        var catalog = showOverviewCatalog ?? (OverviewMode && (Snapshot?.Layout.Overview.Count ?? 0) > 0);
        var subtitle = statusSubtitle;
        if (subtitle is null && CompactLayout && Snapshot is { } snap)
            subtitle = ChatIntercomChromeStatusPresentation.FormatSubtitle(snap, OverviewMode, DetailThreadId);
        var chromeTop = SkiaChatChromeRenderer.ResolveTopChromeHeight(
            CompactLayout,
            catalog,
            !string.IsNullOrWhiteSpace(subtitle));
        var chromeBottom = bottomChrome ?? (float)ResolveBottomChromeHeight((float)Math.Max(160, Bounds.Width));
        var viewport = Math.Max(1, Bounds.Height - chromeTop - chromeBottom);
        var max = Math.Max(0, _cachedContentHeight - viewport);
        if (_scrollOffset > max)
            _scrollOffset = max;
        if (_scrollOffset < 0)
            _scrollOffset = 0;
    }

}
