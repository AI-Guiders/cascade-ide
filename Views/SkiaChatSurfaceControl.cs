#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using CascadeIDE.Features.Chat;
using SkiaSharp;

namespace CascadeIDE.Views;

/// <summary>
/// Skia-центричный chat surface: overview веток, секции тредов и карточки сообщений/уточнений.
/// </summary>
public sealed class SkiaChatSurfaceControl : Control
{
    private const double WheelPixelsPerDelta = 48;

    private readonly List<HitTarget> _hitTargets = [];
    private double _scrollOffset;
    private double _cachedContentHeight;
    private int _hoveredItem = -1;
    private SkiaChatTheme _theme = SkiaChatTheme.DarkFallback;

    public static readonly StyledProperty<ChatSurfaceSnapshot> SnapshotProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, ChatSurfaceSnapshot>(nameof(Snapshot), ChatSurfaceSnapshot.Empty);

    public static readonly StyledProperty<int> SelectedMessageIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(SelectedMessageIndex), -1);

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

    static SkiaChatSurfaceControl()
    {
        AffectsRender<SkiaChatSurfaceControl>(SnapshotProperty, SelectedMessageIndexProperty);
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
        if (change.Property == SnapshotProperty)
        {
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

        var snapshot = BuildItemsSnapshot();
        var width = Math.Max(160, Bounds.Width);
        var contentWidth = (float)(width - 24);
        var maxChars = Math.Max(18, (int)(contentWidth / 7.1f));
        var layouts = SkiaChatLayout.BuildLayout(snapshot, contentWidth, maxChars);
        _cachedContentHeight = SkiaChatLayout.TotalHeight(layouts, snapshot.Length);
        ClampScrollToContent();

        context.Custom(new DrawOperation(
            new Rect(Bounds.Size),
            snapshot,
            layouts,
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

    private void RefreshTheme()
    {
        _theme = SkiaChatTheme.Resolve(this);
    }

    private ItemSnapshot[] BuildItemsSnapshot()
    {
        var snapshot = Snapshot ?? ChatSurfaceSnapshot.Empty;
        var items = new List<ItemSnapshot>();

        if (snapshot.Layout.Overview.Count > 0)
        {
            var overviewText = string.Join(" | ", snapshot.Layout.Overview.Select(item =>
            {
                var prefix = item.Depth > 0 ? new string('>', item.Depth) + " " : "";
                var suffix = item.IsActive ? " [active]" : "";
                return $"{prefix}{item.Title} ({item.ItemCount}){suffix}";
            }));
            items.Add(new ItemSnapshot(
                ItemKind.Overview,
                "Сессия",
                string.IsNullOrWhiteSpace(overviewText) ? "Пока нет веток." : overviewText,
                Accent: "overview",
                MessageIndex: null,
                IsSelected: false,
                IsPending: false,
                StartsBranch: false));
        }

        foreach (var lane in snapshot.Layout.Lanes.OrderBy(lane => lane.Thread.Order))
        {
            var meta = lane.Thread.IsMainThread ? "основная линия" : $"ветка depth {lane.Thread.Depth}";
            if (lane.Thread.IsActive)
                meta += " | активная";
            items.Add(new ItemSnapshot(
                ItemKind.ThreadHeader,
                lane.Thread.Title,
                meta,
                Accent: lane.Thread.IsActive ? "active-thread" : "thread",
                MessageIndex: null,
                IsSelected: false,
                IsPending: false,
                StartsBranch: false));

            items.AddRange(lane.Entries.Select(entry => new ItemSnapshot(
                entry.Kind == ChatSurfaceEntryKind.Message ? ItemKind.Message : ItemKind.Confirmation,
                entry.Title,
                entry.Body,
                entry.Accent,
                entry.MessageIndex,
                entry.IsSelected,
                entry.IsPending,
                entry.StartsBranch)));
        }

        return [.. items];
    }

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
        if (_hitTargets[index].MessageIndex is { } messageIndex)
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

    private enum ItemKind
    {
        Overview = 0,
        ThreadHeader = 1,
        Message = 2,
        Confirmation = 3
    }

    private sealed record ItemSnapshot(
        ItemKind Kind,
        string Title,
        string Content,
        string Accent,
        int? MessageIndex,
        bool IsSelected,
        bool IsPending,
        bool StartsBranch);

    private sealed record HitTarget(Rect Bounds, int? MessageIndex);

    private readonly record struct BubbleLayout(float Top, float Height, IReadOnlyList<string> ContentLines);

    private readonly record struct SkiaChatTheme(
        SKColor Surface,
        SKColor BubbleAssistant,
        SKColor BubbleUser,
        SKColor Border,
        SKColor HoverBorder,
        SKColor SelectedBorder,
        SKColor Role,
        SKColor Content,
        SKColor EmptyHint)
    {
        public static SkiaChatTheme DarkFallback => new(
            Surface: new SKColor(37, 37, 38),
            BubbleAssistant: new SKColor(45, 47, 55),
            BubbleUser: new SKColor(53, 72, 112),
            Border: new SKColor(84, 92, 108),
            HoverBorder: new SKColor(126, 196, 255),
            SelectedBorder: new SKColor(196, 146, 255),
            Role: new SKColor(181, 196, 230),
            Content: new SKColor(223, 228, 236),
            EmptyHint: new SKColor(160, 160, 160));

        public static SkiaChatTheme Resolve(StyledElement element)
        {
            var theme = DarkFallback;
            if (TrySkColor(element, "CascadeTheme.ChatMessageBubbleBackground", out var bubble))
                theme = theme with { BubbleAssistant = bubble, Surface = Darken(bubble, 0.92f) };
            if (TrySkColor(element, "CascadeTheme.ChatLabelForeground", out var label))
                theme = theme with { Role = label, EmptyHint = label };
            if (TrySkColor(element, "CascadeTheme.ChatMessageContentForeground", out var body))
                theme = theme with { Content = body };
            if (TrySkColor(element, "CascadeTheme.EditorColumnBorderBrush", out var edge))
            {
                theme = theme with { Border = edge };
                theme = theme with { BubbleUser = Blend(theme.BubbleAssistant, edge, 0.42f) };
            }

            if (TrySkColor(element, "CascadeTheme.PanelChromeAccentBrush", out var accent))
            {
                theme = theme with { HoverBorder = accent };
                theme = theme with { SelectedBorder = Blend(accent, new SKColor(255, 255, 255), 0.35f) };
            }

            return theme;
        }

        private static SKColor Darken(SKColor color, float amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            return new SKColor(
                (byte)(color.Red * amount),
                (byte)(color.Green * amount),
                (byte)(color.Blue * amount),
                color.Alpha);
        }

        public static SKColor Blend(SKColor a, SKColor b, float t)
        {
            t = Math.Clamp(t, 0, 1);
            return new SKColor(
                (byte)(a.Red + (b.Red - a.Red) * t),
                (byte)(a.Green + (b.Green - a.Green) * t),
                (byte)(a.Blue + (b.Blue - a.Blue) * t),
                (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
        }

        private static bool TrySkColor(StyledElement element, string key, out SKColor color)
        {
            if (element.TryGetResource(key, element.ActualThemeVariant, out var resource) && resource is IBrush brush)
            {
                color = BrushToSkColor(brush);
                return true;
            }

            color = default;
            return false;
        }

        private static SKColor BrushToSkColor(IBrush brush)
        {
            if (brush is SolidColorBrush solid)
                return new SKColor(solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A);
            return new SKColor(45, 45, 48);
        }
    }

    private static class SkiaChatLayout
    {
        public static List<BubbleLayout> BuildLayout(ItemSnapshot[] items, float contentWidth, int maxChars)
        {
            var layouts = new List<BubbleLayout>();
            var y = 8f;
            foreach (var item in items)
            {
                var lines = WrapChatText(TrimChatText(item.Content, 32_000), maxChars);
                var titleHeight = string.IsNullOrWhiteSpace(item.Title) ? 0 : 16;
                var boxHeight = 18 + titleHeight + Math.Max(1, lines.Count) * 16 + 10;
                layouts.Add(new BubbleLayout(y, boxHeight, lines));
                y += boxHeight + 8;
            }

            return layouts;
        }

        public static double TotalHeight(IReadOnlyList<BubbleLayout> layouts, int itemCount)
        {
            if (itemCount == 0)
                return 56;
            var last = layouts[^1];
            return last.Top + last.Height + 8;
        }
    }

    private static string TrimChatText(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen] + "...";

    private static List<string> WrapChatText(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [""];

        var words = text.Replace("\r", "").Replace("\n", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = "";
        foreach (var word in words)
        {
            if (current.Length == 0)
            {
                current = word;
                continue;
            }

            if (current.Length + 1 + word.Length <= maxChars)
            {
                current += " " + word;
                continue;
            }

            lines.Add(current);
            current = word;
        }

        if (current.Length > 0)
            lines.Add(current);
        return lines;
    }

    private sealed class DrawOperation : ICustomDrawOperation
    {
        private readonly ItemSnapshot[] _items;
        private readonly IReadOnlyList<BubbleLayout> _layouts;
        private readonly float _scrollOffset;
        private readonly SkiaChatTheme _theme;
        private readonly int _selectedMessageIndex;
        private readonly int _hoveredItem;
        private readonly List<HitTarget> _hitTargets;

        public DrawOperation(
            Rect bounds,
            ItemSnapshot[] items,
            IReadOnlyList<BubbleLayout> layouts,
            float scrollOffset,
            SkiaChatTheme theme,
            int selectedMessageIndex,
            int hoveredItem,
            List<HitTarget> hitTargets)
        {
            Bounds = bounds;
            _items = items;
            _layouts = layouts;
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
            var x = 12f;
            var contentWidth = width - 24f;
            canvas.ClipRect(new SKRect(0, 0, width, height), antialias: false);
            canvas.Translate(0, -_scrollOffset);

            using var stroke = new SKPaint { Color = _theme.Border, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            using var hoverStroke = new SKPaint { Color = _theme.HoverBorder, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var selectedStroke = new SKPaint { Color = _theme.SelectedBorder, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.2f };
            using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 10);
            using var titlePaint = new SKPaint { IsAntialias = true, Color = _theme.Role };
            using var contentFont = new SKFont(SKTypeface.FromFamilyName("Consolas"), 12);
            using var contentPaint = new SKPaint { IsAntialias = true, Color = _theme.Content };
            using var emptyFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 11);
            using var emptyPaint = new SKPaint { IsAntialias = true, Color = _theme.EmptyHint };

            _hitTargets.Clear();

            if (_items.Length == 0)
            {
                canvas.DrawText("Пока пусто. Задай вопрос или команду.", x, 28, SKTextAlign.Left, emptyFont, emptyPaint);
                canvas.Restore();
                return;
            }

            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                var layout = _layouts[i];
                var rect = new SKRect(x, layout.Top, x + contentWidth, layout.Top + layout.Height);

                using var fill = new SKPaint
                {
                    Color = ResolveBubbleColor(item),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRoundRect(rect, 7, 7, fill);
                canvas.DrawRoundRect(rect, 7, 7, stroke);
                if (_hoveredItem == i)
                    canvas.DrawRoundRect(rect, 7, 7, hoverStroke);
                if (item.IsSelected || (item.MessageIndex is not null && item.MessageIndex == _selectedMessageIndex))
                    canvas.DrawRoundRect(rect, 7, 7, selectedStroke);

                canvas.DrawText(item.Title, x + 10, layout.Top + 14, SKTextAlign.Left, titleFont, titlePaint);
                var textY = layout.Top + 30;
                foreach (var line in layout.ContentLines)
                {
                    canvas.DrawText(line, x + 10, textY, SKTextAlign.Left, contentFont, contentPaint);
                    textY += 16;
                }

                _hitTargets.Add(new HitTarget(
                    new Rect(rect.Left, rect.Top - _scrollOffset, rect.Width, rect.Height),
                    item.MessageIndex));
            }

            canvas.Restore();
        }

        public bool HitTest(Point p) => Bounds.Contains(p);

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose()
        {
        }

        private SKColor ResolveBubbleColor(ItemSnapshot item)
        {
            return item.Kind switch
            {
                ItemKind.Overview => SkiaChatTheme.Blend(_theme.Surface, _theme.Border, 0.45f),
                ItemKind.ThreadHeader => SkiaChatTheme.Blend(_theme.Surface, _theme.HoverBorder, 0.22f),
                ItemKind.Confirmation => SkiaChatTheme.Blend(_theme.BubbleAssistant, _theme.HoverBorder, item.IsPending ? 0.32f : 0.18f),
                _ => string.Equals(item.Accent, "user", StringComparison.OrdinalIgnoreCase)
                    ? _theme.BubbleUser
                    : item.StartsBranch
                        ? SkiaChatTheme.Blend(_theme.BubbleAssistant, _theme.SelectedBorder, 0.24f)
                        : _theme.BubbleAssistant
            };
        }
    }
}
