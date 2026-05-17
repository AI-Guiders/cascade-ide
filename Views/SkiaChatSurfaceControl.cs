#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using CascadeIDE.Features.Chat;
using CascadeIDE.Views.Chat;
using CascadeIDE.Views.Chat.Skia;
using SkiaSharp;

namespace CascadeIDE.Views;

/// <summary>
/// Skia-центричный chat surface: overview веток, секции тредов и карточки сообщений/уточнений.
/// </summary>
public sealed class SkiaChatSurfaceControl : Control
{
    private const double WheelPixelsPerDelta = 48;

    private readonly List<(Rect Bounds, SkiaChatHit Hit)> _hitTargets = [];
    private double _scrollOffset;
    private double _cachedContentHeight;
    private int _hoveredItem = -1;
    private SkiaChatTheme _theme = SkiaChatTheme.DarkFallback;

    public static readonly StyledProperty<ChatSurfaceSnapshot> SnapshotProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, ChatSurfaceSnapshot>(nameof(Snapshot), ChatSurfaceSnapshot.Empty);

    public static readonly StyledProperty<int> SelectedMessageIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(SelectedMessageIndex), -1);

    public static readonly StyledProperty<Guid> DetailThreadIdProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, Guid>(nameof(DetailThreadId), Guid.Empty);

    public static readonly StyledProperty<bool> OverviewModeProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(OverviewMode), false);

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

    static SkiaChatSurfaceControl()
    {
        AffectsRender<SkiaChatSurfaceControl>(SnapshotProperty, SelectedMessageIndexProperty, DetailThreadIdProperty, OverviewModeProperty);
    }

    public SkiaChatSurfaceControl()
    {
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Bubble);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);
        AddHandler(PointerExitedEvent, OnPointerExited, RoutingStrategies.Bubble);
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Bubble);
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

    public override void Render(DrawingContext context)
    {
        RefreshTheme();

        var snapshot = Snapshot ?? ChatSurfaceSnapshot.Empty;
        var entities = SkiaChatSceneBuilder.Build(snapshot, OverviewMode, DetailThreadId);
        var width = Math.Max(160, Bounds.Width);
        var contentWidth = (float)(width - 24);
        var maxChars = Math.Max(18, (int)(contentWidth / 7.1f));
        var measureContext = new SkiaChatMeasureContext(maxChars, contentWidth);
        var placed = SkiaChatLayoutEngine.Layout(entities, measureContext);
        _cachedContentHeight = SkiaChatLayoutEngine.TotalHeight(placed);

        ClampScrollToContent();

        context.Custom(new DrawOperation(
            new Rect(Bounds.Size),
            placed,
            (float)_scrollOffset,
            _theme,
            SelectedMessageIndex,
            _hoveredItem,
            _hitTargets));
        base.Render(context);
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        RefreshTheme();
        InvalidateVisual();
    }

    private void RefreshTheme() => _theme = SkiaChatTheme.Resolve(this);

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
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
            SelectedMessageIndex = messageIndex;

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

    private void ClampScrollToContent()
    {
        var viewport = Math.Max(1, Bounds.Height);
        var max = Math.Max(0, _cachedContentHeight - viewport);
        if (_scrollOffset > max)
            _scrollOffset = max;
        if (_scrollOffset < 0)
            _scrollOffset = 0;
    }

    private sealed class DrawOperation : ICustomDrawOperation
    {
        private readonly IReadOnlyList<SkiaChatPlacedEntity> _placed;
        private readonly float _scrollOffset;
        private readonly SkiaChatTheme _theme;
        private readonly int _selectedMessageIndex;
        private readonly int _hoveredItem;
        private readonly List<(Rect Bounds, SkiaChatHit Hit)> _hitTargets;

        public DrawOperation(
            Rect bounds,
            IReadOnlyList<SkiaChatPlacedEntity> placed,
            float scrollOffset,
            SkiaChatTheme theme,
            int selectedMessageIndex,
            int hoveredItem,
            List<(Rect Bounds, SkiaChatHit Hit)> hitTargets)
        {
            Bounds = bounds;
            _placed = placed;
            _scrollOffset = scrollOffset;
            _theme = theme;
            _selectedMessageIndex = selectedMessageIndex;
            _hoveredItem = hoveredItem;
            _hitTargets = hitTargets;
        }

        public Rect Bounds { get; }

        public void Render(ImmediateDrawingContext context)
        {
            var feature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
            if (feature is null)
                return;

            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();
            canvas.Clear(_theme.Surface);

            var width = Math.Max(160f, (float)Bounds.Width);
            var height = Math.Max(1f, (float)Bounds.Height);
            const float contentLeft = 12f;
            var contentWidth = width - 24f;
            canvas.ClipRect(new SKRect(0, 0, width, height), antialias: false);
            canvas.Translate(0, -_scrollOffset);

            _hitTargets.Clear();

            if (_placed.Count == 0)
            {
                using var emptyFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 11);
                using var emptyPaint = new SKPaint { IsAntialias = true, Color = _theme.EmptyHint };
                canvas.DrawText("Пока пусто. Задай вопрос или команду.", contentLeft, 28, SKTextAlign.Left, emptyFont, emptyPaint);
                canvas.Restore();
                return;
            }

            for (var i = 0; i < _placed.Count; i++)
            {
                var placed = _placed[i];
                var itemLeft = float.IsNaN(placed.Left) ? contentLeft : placed.Left;
                var itemWidth = float.IsNaN(placed.Width) ? contentWidth : placed.Width;
                var drawContext = new SkiaChatDrawContext
                {
                    Canvas = canvas,
                    Theme = _theme,
                    ContentLeft = itemLeft,
                    ContentWidth = itemWidth,
                    ScrollOffset = _scrollOffset,
                    ItemIndex = i,
                    HoveredItemIndex = _hoveredItem,
                    SelectedMessageIndex = _selectedMessageIndex,
                    HitTargets = _hitTargets
                };
                placed.Entity.Draw(drawContext, placed.Top, placed.Layout);

                var hit = placed.Entity.CreateHit(placed.Layout);
                if (hit is { } h)
                {
                    var rect = new SKRect(itemLeft, placed.Top, itemLeft + itemWidth, placed.Top + placed.Layout.Height);
                    drawContext.RegisterHit(rect, h);
                }
            }

            canvas.Restore();
        }

        public bool HitTest(Point p) => Bounds.Contains(p);

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose()
        {
        }
    }
}
