#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views;

/// <summary>
/// Skia-центричный chat surface: overview веток, секции тредов и карточки сообщений/уточнений.
/// </summary>
public partial class SkiaChatSurfaceControl : Control
{
    private const double WheelPixelsPerDelta = 48;

    private readonly SkiaChatHitRegistry _chatHits = new();
    private double _scrollOffset;
    private double _cachedContentHeight;
    private int _hoveredItem = -1;
    private SkiaChatTheme _theme = SkiaChatTheme.DarkFallback;
    private WriteableBitmap? _skiaFrame;
    private int _skiaFrameWidth;
    private int _skiaFrameHeight;
    private float _skiaFrameLayoutScale = 1f;

    public static readonly StyledProperty<ChatSurfaceSnapshot> SnapshotProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, ChatSurfaceSnapshot>(nameof(Snapshot), ChatSurfaceSnapshot.Empty);

    public static readonly StyledProperty<int> SelectedMessageIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(SelectedMessageIndex), -1);

    public static readonly StyledProperty<Guid> DetailThreadIdProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, Guid>(nameof(DetailThreadId), Guid.Empty);

    public static readonly StyledProperty<bool> OverviewModeProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(OverviewMode), false);

    public static readonly StyledProperty<bool> ForwardHostProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(ForwardHost), false);

    /// <summary>Типографика Skia-ленты из <c>[fonts.intercom]</c>.</summary>
    public static readonly StyledProperty<IntercomFontsSettings> IntercomFontsProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, IntercomFontsSettings>(
            nameof(IntercomFonts),
            new IntercomFontsSettings());

    public static readonly StyledProperty<string> ChromeTitleProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(nameof(ChromeTitle), "Intercom");

    public static readonly StyledProperty<string> LoadingStatusTextProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string>(nameof(LoadingStatusText), "");

    public static readonly StyledProperty<bool> IsChatLoadingProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(IsChatLoading), false);

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

    public bool ForwardHost
    {
        get => GetValue(ForwardHostProperty);
        set => SetValue(ForwardHostProperty, value);
    }

    public IntercomFontsSettings IntercomFonts
    {
        get => GetValue(IntercomFontsProperty);
        set => SetValue(IntercomFontsProperty, value);
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
        FocusableProperty.OverrideDefaultValue<SkiaChatSurfaceControl>(true);
        AffectsRender<SkiaChatSurfaceControl>(
            SnapshotProperty,
            SelectedMessageIndexProperty,
            DetailThreadIdProperty,
            OverviewModeProperty,
            ForwardHostProperty,
            IntercomFontsProperty,
            ChromeTitleProperty,
            LoadingStatusTextProperty,
            IsChatLoadingProperty,
            ShowIntercomComposerProperty,
            ComposerTextProperty,
            ComposerCaretIndexProperty,
            ComposerPreeditTextProperty,
            IsComposerEnabledProperty,
            IsSlashAutocompleteVisibleProperty,
            SelectedSlashSuggestionIndexProperty,
            SlashSuggestionsProperty,
            ShowCockpitCommandLineProperty,
            CommandLineTextProperty,
            CommandLinePreviewProperty,
            CommandLineCaretIndexProperty);

        ShowCockpitCommandLineProperty.Changed.AddClassHandler<SkiaChatSurfaceControl>(OnShowCockpitCommandLineChanged);
    }

    private static void OnShowCockpitCommandLineChanged(SkiaChatSurfaceControl control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            control._commandLineFocused = true;
            control.InvalidateVisual();
        }
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
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
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
        StopComposerCaretBlink();
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
        var entities = SkiaChatSceneBuilder.Build(
            snapshot,
            OverviewMode,
            DetailThreadId,
            ForwardHost,
            IntercomFonts);
        var width = Math.Max(160, Bounds.Width);
        var showFeedGutterForMeasure = !OverviewMode
            && (snapshot.Layout.Lanes.Any(l => l.Entries.Any(e => e.Kind == ChatSurfaceEntryKind.Message)));
        var gutterPadForMeasure = showFeedGutterForMeasure ? SkiaChatDrawContext.FeedGutterWidth : 0f;
        var contentWidth = (float)(width - 24 - gutterPadForMeasure);
        var maxChars = Math.Max(18, (int)(contentWidth / 7.1f));
        var measureContext = new SkiaChatMeasureContext(maxChars, contentWidth);
        var placed = SkiaChatLayoutEngine.Layout(entities, measureContext);
        _cachedContentHeight = SkiaChatLayoutEngine.TotalHeight(placed);

        var showOverviewCatalog = OverviewMode && snapshot.Layout.Overview.Count > 0;
        var statusSubtitle = ForwardHost
            ? ChatIntercomChromeStatusPresentation.FormatSubtitle(snapshot, OverviewMode, DetailThreadId)
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

    private static bool IsIntercomSkiaRenderingEnabled() =>
        !string.Equals(
            Environment.GetEnvironmentVariable("CASCADE_INTERCOM_SKIA"),
            "0",
            StringComparison.Ordinal);

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

        var chromeTop = SkiaChatChromeRenderer.ResolveTopChromeHeight(
            ForwardHost,
            showOverviewCatalog,
            !string.IsNullOrWhiteSpace(statusSubtitle));
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
                out var overviewBounds,
                IntercomFonts);
            registerChromePointerHits(overviewBounds);
        }

        if (showOverviewCatalog)
        {
            var bandTop = ForwardHost
                ? SkiaChatChromeRenderer.ResolveToolbarHeight(true, !string.IsNullOrWhiteSpace(statusSubtitle))
                : 0f;
            SkiaChatChromeRenderer.DrawOverviewCatalogBand(canvas, width, bandTop, _theme, overviewTopicCount, IntercomFonts);
        }

        var showFeedGutter = !OverviewMode && (Snapshot?.Layout.Lanes.Any(l => l.Entries.Any(e => e.Kind == ChatSurfaceEntryKind.Message)) ?? false);
        var gutterPad = showFeedGutter ? SkiaChatDrawContext.FeedGutterWidth : 0f;
        const float contentLeftBase = 12f;
        var contentLeft = contentLeftBase + gutterPad;
        var contentWidth = width - 24f - gutterPad;
        var contentBottom = Math.Max(chromeTop + 1f, height - bottomChrome);

        canvas.Save();
        canvas.ClipRect(new SKRect(0, chromeTop, width, contentBottom), antialias: false);
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

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || (e.KeyModifiers & KeyModifiers.Control) == 0)
            return;

        if (!await TryCopySelectedMessageAsync().ConfigureAwait(true))
            return;

        e.Handled = true;
    }

    private async Task<bool> TryCopySelectedMessageAsync()
    {
        if (SelectedMessageIndex < 0)
            return false;

        var body = ChatSurfaceSnapshotMessageLookup.TryGetMessageBody(Snapshot, SelectedMessageIndex);
        if (string.IsNullOrEmpty(body))
            return false;

        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard is not { } clipboard)
            return false;

        await clipboard.SetTextAsync(body).ConfigureAwait(true);
        return true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var p = e.GetPosition(this);
        if (TryDispatchPointerWheel(p, e))
        {
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        _scrollOffset -= e.Delta.Y * WheelPixelsPerDelta;
        ClampScrollToContent();
        e.Handled = true;
        InvalidateVisual();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var index = _chatHits.FindIndex(e.GetPosition(this));
        if (index == _hoveredItem)
            return;
        _hoveredItem = index;
        var hand = index >= 0 && _chatHits.TryGetHit(index, out var hoverHit) && SkiaChatHitRegistry.WantsHandCursor(hoverHit);
        Cursor = hand ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow);
        InvalidateVisual();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!TryDispatchPointerPress(e.GetPosition(this), e))
            return;

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

    private void ClampScrollToContent(bool? showOverviewCatalog = null, string? statusSubtitle = null, float? bottomChrome = null)
    {
        var catalog = showOverviewCatalog ?? (OverviewMode && (Snapshot?.Layout.Overview.Count ?? 0) > 0);
        var subtitle = statusSubtitle;
        if (subtitle is null && ForwardHost && Snapshot is { } snap)
            subtitle = ChatIntercomChromeStatusPresentation.FormatSubtitle(snap, OverviewMode, DetailThreadId);
        var chromeTop = SkiaChatChromeRenderer.ResolveTopChromeHeight(
            ForwardHost,
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
