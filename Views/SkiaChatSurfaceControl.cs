using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using CascadeIDE.ViewModels;
using SkiaSharp;

namespace CascadeIDE.Views;

/// <summary>
/// Лента чата на Skia: скролл, тема из <see cref="TryGetResource"/>, выбор сообщения по клику.
/// </summary>
public sealed class SkiaChatSurfaceControl : Control
{
    private const double WheelPixelsPerDelta = 48;

    private readonly List<BubbleHit> _bubbleHits = [];
    private int _hoveredBubble = -1;
    private int _selectedBubble = -1;
    private INotifyCollectionChanged? _messagesCollection;
    private readonly List<ChatMessageViewModel> _wiredMessages = [];
    private double _scrollOffset;
    private double _cachedContentHeight;
    private SkiaChatTheme _theme;

    public static readonly StyledProperty<IEnumerable<ChatMessageViewModel>?> MessagesProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, IEnumerable<ChatMessageViewModel>?>(nameof(Messages));

    public static readonly StyledProperty<bool> HasActiveClarificationProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, bool>(nameof(HasActiveClarification));

    public static readonly StyledProperty<string?> ClarificationTitleProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, string?>(nameof(ClarificationTitle));

    public static readonly StyledProperty<int> SelectedMessageIndexProperty =
        AvaloniaProperty.Register<SkiaChatSurfaceControl, int>(nameof(SelectedMessageIndex), -1);

    public IEnumerable<ChatMessageViewModel>? Messages
    {
        get => GetValue(MessagesProperty);
        set => SetValue(MessagesProperty, value);
    }

    public bool HasActiveClarification
    {
        get => GetValue(HasActiveClarificationProperty);
        set => SetValue(HasActiveClarificationProperty, value);
    }

    public string? ClarificationTitle
    {
        get => GetValue(ClarificationTitleProperty);
        set => SetValue(ClarificationTitleProperty, value);
    }

    public int SelectedMessageIndex
    {
        get => GetValue(SelectedMessageIndexProperty);
        set => SetValue(SelectedMessageIndexProperty, value);
    }

    static SkiaChatSurfaceControl()
    {
        AffectsRender<SkiaChatSurfaceControl>(
            MessagesProperty,
            HasActiveClarificationProperty,
            ClarificationTitleProperty,
            SelectedMessageIndexProperty);
    }

    public SkiaChatSurfaceControl()
    {
        _theme = SkiaChatTheme.DarkFallback;
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
        UnwireMessages();
        base.OnDetachedFromVisualTree(e);
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MessagesProperty)
            RebindMessages(change.NewValue as IEnumerable<ChatMessageViewModel>);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ClampScrollToContent();
        InvalidateVisual();
    }

    private void RebindMessages(IEnumerable<ChatMessageViewModel>? newMsgs)
    {
        UnwireMessages();
        if (newMsgs is null)
            return;
        if (newMsgs is INotifyCollectionChanged ncc)
        {
            _messagesCollection = ncc;
            ncc.CollectionChanged += OnMessagesCollectionChanged;
        }

        foreach (var vm in newMsgs)
            WireMessage(vm);
    }

    private void UnwireMessages()
    {
        if (_messagesCollection is not null)
        {
            _messagesCollection.CollectionChanged -= OnMessagesCollectionChanged;
            _messagesCollection = null;
        }

        foreach (var vm in _wiredMessages.ToArray())
        {
            vm.PropertyChanged -= OnMessageVmPropertyChanged;
            _wiredMessages.Remove(vm);
        }
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var viewport = Math.Max(1, Bounds.Height);
        var prevContent = _cachedContentHeight;
        var oldMax = Math.Max(0, prevContent - viewport);

        ResyncMessageItemSubscriptions();

        var newContent = MeasureContentHeight();
        var newMax = Math.Max(0, newContent - viewport);
        if (_scrollOffset >= oldMax - 2 || oldMax < 1)
            _scrollOffset = newMax;
        else
            _scrollOffset = Math.Min(_scrollOffset, newMax);

        InvalidateVisual();
    }

    private void ResyncMessageItemSubscriptions()
    {
        foreach (var vm in _wiredMessages.ToArray())
        {
            vm.PropertyChanged -= OnMessageVmPropertyChanged;
            _wiredMessages.Remove(vm);
        }

        if (Messages is null)
            return;
        foreach (var vm in Messages)
            WireMessage(vm);
    }

    private void WireMessage(ChatMessageViewModel vm)
    {
        if (_wiredMessages.Contains(vm))
            return;
        vm.PropertyChanged += OnMessageVmPropertyChanged;
        _wiredMessages.Add(vm);
    }

    private void OnMessageVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null
            || e.PropertyName == nameof(ChatMessageViewModel.Content)
            || e.PropertyName == nameof(ChatMessageViewModel.Role))
            InvalidateVisual();
    }

    private double MeasureContentHeight()
    {
        var width = Math.Max(120, Bounds.Width);
        var contentWidth = (float)(width - 24);
        var maxChars = Math.Max(16, (int)(contentWidth / 7.2f));
        var snapshot = BuildSnapshot();
        return SkiaChatLayout.TotalHeight(SkiaChatLayout.BuildLayout(snapshot, contentWidth, maxChars), snapshot.Length);
    }

    private MessageSnapshot[] BuildSnapshot()
    {
        if (Messages is null)
            return [];
        return Messages.Select(m => new MessageSnapshot(m.Role ?? "", m.Content ?? "")).ToArray();
    }

    private void ClampScrollToContent()
    {
        var viewport = Math.Max(1, Bounds.Height);
        var content = MeasureContentHeight();
        var max = Math.Max(0, content - viewport);
        if (_scrollOffset > max)
            _scrollOffset = max;
        if (_scrollOffset < 0)
            _scrollOffset = 0;
    }

    public override void Render(DrawingContext context)
    {
        var snapshot = BuildSnapshot();
        var width = Math.Max(120, Bounds.Width);
        var contentWidth = (float)(width - 24);
        var maxChars = Math.Max(16, (int)(contentWidth / 7.2f));
        var layouts = SkiaChatLayout.BuildLayout(snapshot, contentWidth, maxChars);
        _cachedContentHeight = SkiaChatLayout.TotalHeight(layouts, snapshot.Length);
        ClampScrollToContent();

        RefreshTheme();

        context.Custom(new DrawOperation(
            new Rect(Bounds.Size),
            snapshot,
            layouts,
            (float)_scrollOffset,
            _theme,
            SelectedMessageIndex >= 0 ? SelectedMessageIndex : _selectedBubble,
            _hoveredBubble,
            _bubbleHits));
        base.Render(context);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y * WheelPixelsPerDelta;
        _scrollOffset -= delta;
        ClampScrollToContent();
        e.Handled = true;
        InvalidateVisual();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var p = e.GetPosition(this);
        var index = FindHit(p);
        if (index == _hoveredBubble)
            return;
        _hoveredBubble = index;
        Cursor = _hoveredBubble >= 0 ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow);
        InvalidateVisual();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var p = e.GetPosition(this);
        var index = FindHit(p);
        if (index < 0)
            return;
        _selectedBubble = index;
        SelectedMessageIndex = index;
        InvalidateVisual();
        e.Handled = true;
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (_hoveredBubble < 0)
            return;
        _hoveredBubble = -1;
        Cursor = new Cursor(StandardCursorType.Arrow);
        InvalidateVisual();
    }

    private int FindHit(Point p)
    {
        for (var i = 0; i < _bubbleHits.Count; i++)
        {
            if (_bubbleHits[i].Bounds.Contains(p))
                return _bubbleHits[i].Index;
        }

        return -1;
    }

    private sealed record MessageSnapshot(string Role, string Content);

    private sealed record BubbleHit(Rect Bounds, int Index);

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

        public static SkiaChatTheme Resolve(StyledElement el)
        {
            var t = DarkFallback;
            if (TrySkColor(el, "CascadeTheme.ChatMessageBubbleBackground", out var bubble))
                t = t with { BubbleAssistant = bubble, Surface = Darken(bubble, 0.92f) };
            if (TrySkColor(el, "CascadeTheme.ChatLabelForeground", out var label))
                t = t with { Role = label, EmptyHint = label };
            if (TrySkColor(el, "CascadeTheme.ChatMessageContentForeground", out var body))
                t = t with { Content = body };
            if (TrySkColor(el, "CascadeTheme.EditorColumnBorderBrush", out var edge))
            {
                t = t with { Border = edge };
                t = t with { BubbleUser = Blend(t.BubbleAssistant, edge, 0.42f) };
            }

            if (TrySkColor(el, "CascadeTheme.PanelChromeAccentBrush", out var accent))
            {
                t = t with { HoverBorder = accent };
                t = t with { SelectedBorder = Blend(accent, new SKColor(255, 255, 255), 0.35f) };
            }

            return t;
        }

        private static SKColor Darken(SKColor c, float amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            return new SKColor(
                (byte)(c.Red * amount),
                (byte)(c.Green * amount),
                (byte)(c.Blue * amount),
                c.Alpha);
        }

        private static SKColor Blend(SKColor a, SKColor b, float t)
        {
            t = Math.Clamp(t, 0, 1);
            return new SKColor(
                (byte)(a.Red + (b.Red - a.Red) * t),
                (byte)(a.Green + (b.Green - a.Green) * t),
                (byte)(a.Blue + (b.Blue - a.Blue) * t),
                (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
        }

        private static bool TrySkColor(StyledElement el, string key, out SKColor color)
        {
            if (el.TryGetResource(key, el.ActualThemeVariant, out var o) && o is IBrush b)
            {
                color = BrushToSkColor(b);
                return true;
            }

            color = default;
            return false;
        }

        private static SKColor BrushToSkColor(IBrush brush)
        {
            if (brush is SolidColorBrush scb)
                return new SKColor(scb.Color.R, scb.Color.G, scb.Color.B, scb.Color.A);
            return new SKColor(45, 45, 48);
        }
    }

    private static class SkiaChatLayout
    {
        public static List<BubbleLayout> BuildLayout(
            MessageSnapshot[] messages,
            float contentWidth,
            int maxChars)
        {
            var list = new List<BubbleLayout>();
            var y = 8f;
            for (var i = 0; i < messages.Length; i++)
            {
                var lines = WrapChatText(TrimChatText(messages[i].Content, 32_000), maxChars);
                var lineCount = Math.Max(1, lines.Count);
                var boxHeight = 22 + lineCount * 16 + 8;
                list.Add(new BubbleLayout(i, y, boxHeight));
                y += boxHeight + 8;
            }

            return list;
        }

        public static double TotalHeight(List<BubbleLayout> layouts, int messageCount)
        {
            if (messageCount == 0)
                return 48;
            return layouts[^1].Top + layouts[^1].Height + 8;
        }
    }

    private readonly record struct BubbleLayout(int Index, float Top, float Height);

    private static string TrimChatText(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen] + "...";

    private static List<string> WrapChatText(string text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [""];
        var words = text.Replace("\r", "").Replace("\n", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = "";
        foreach (var w in words)
        {
            if (current.Length == 0)
            {
                current = w;
                continue;
            }

            if (current.Length + 1 + w.Length <= maxChars)
            {
                current += " " + w;
                continue;
            }

            lines.Add(current);
            current = w;
        }

        if (current.Length > 0)
            lines.Add(current);
        return lines;
    }

    private sealed class DrawOperation : ICustomDrawOperation
    {
        private readonly MessageSnapshot[] _messages;
        private readonly List<BubbleLayout> _layouts;
        private readonly float _scrollOffset;
        private readonly SkiaChatTheme _theme;
        private readonly int _selectedBubble;
        private readonly int _hoveredBubble;
        private readonly List<BubbleHit> _hitTargetSink;

        public DrawOperation(
            Rect bounds,
            MessageSnapshot[] messages,
            List<BubbleLayout> layouts,
            float scrollOffset,
            SkiaChatTheme theme,
            int selectedBubble,
            int hoveredBubble,
            List<BubbleHit> hitTargetSink)
        {
            Bounds = bounds;
            _messages = messages;
            _layouts = layouts;
            _scrollOffset = scrollOffset;
            _theme = theme;
            _selectedBubble = selectedBubble;
            _hoveredBubble = hoveredBubble;
            _hitTargetSink = hitTargetSink;
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

            var width = Math.Max(120f, (float)Bounds.Width);
            var height = Math.Max(1f, (float)Bounds.Height);
            var x = 12f;
            var contentWidth = width - 24f;

            canvas.ClipRect(new SKRect(0, 0, width, height), antialias: false);
            canvas.Translate(0, -_scrollOffset);

            using var stroke = new SKPaint { Color = _theme.Border, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            using var hoverStroke = new SKPaint { Color = _theme.HoverBorder, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var selectedStroke = new SKPaint { Color = _theme.SelectedBorder, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.2f };
            using var roleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 10);
            using var rolePaint = new SKPaint { IsAntialias = true, Color = _theme.Role };
            using var contentFont = new SKFont(SKTypeface.FromFamilyName("Consolas"), 12);
            using var contentPaint = new SKPaint { IsAntialias = true, Color = _theme.Content };
            using var emptyFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 11);
            using var emptyPaint = new SKPaint { IsAntialias = true, Color = _theme.EmptyHint };

            _hitTargetSink.Clear();

            if (_messages.Length == 0)
            {
                canvas.DrawText("Пока пусто. Задай вопрос или команду.", x, 28, SKTextAlign.Left, emptyFont, emptyPaint);
                canvas.Restore();
                return;
            }

            var maxChars = Math.Max(16, (int)(contentWidth / 7.2f));
            for (var i = 0; i < _messages.Length && i < _layouts.Count; i++)
            {
                var msg = _messages[i];
                var layout = _layouts[i];
                var lines = WrapChatText(TrimChatText(msg.Content, 32_000), maxChars);
                var r = new SKRect(x, layout.Top, x + contentWidth, layout.Top + layout.Height);
                var bg = string.Equals(msg.Role, "user", StringComparison.OrdinalIgnoreCase)
                    ? _theme.BubbleUser
                    : _theme.BubbleAssistant;
                canvas.DrawRoundRect(r, 7, 7, new SKPaint { Color = bg, IsAntialias = true });
                canvas.DrawRoundRect(r, 7, 7, stroke);
                if (_hoveredBubble == i)
                    canvas.DrawRoundRect(r, 7, 7, hoverStroke);
                if (_selectedBubble == i)
                    canvas.DrawRoundRect(r, 7, 7, selectedStroke);
                canvas.DrawText(msg.Role.ToUpperInvariant(), x + 10, layout.Top + 14, SKTextAlign.Left, roleFont, rolePaint);
                var textY = layout.Top + 30;
                foreach (var line in lines)
                {
                    canvas.DrawText(line, x + 10, textY, SKTextAlign.Left, contentFont, contentPaint);
                    textY += 16;
                }

                var topInControl = layout.Top - _scrollOffset;
                _hitTargetSink.Add(new BubbleHit(new Rect(r.Left, topInControl, r.Width, r.Height), i));
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
