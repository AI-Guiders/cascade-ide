using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using CascadeIDE.ViewModels;
using SkiaSharp;
using System.Linq;

namespace CascadeIDE.Views;

/// <summary>
/// Skia-spike для чата: отдельный рендер ленты и активного clarification-блока.
/// Не заменяет каноничную модель, только визуальный эксперимент.
/// </summary>
public sealed class SkiaChatSurfaceControl : Control
{
    private readonly List<BubbleHit> _bubbleHits = [];
    private int _hoveredBubble = -1;
    private int _selectedBubble = -1;

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
        AffectsRender<SkiaChatSurfaceControl>(MessagesProperty, HasActiveClarificationProperty, ClarificationTitleProperty, SelectedMessageIndexProperty);
    }

    public SkiaChatSurfaceControl()
    {
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Bubble);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Bubble);
        AddHandler(PointerExitedEvent, OnPointerExited, RoutingStrategies.Bubble);
    }

    public override void Render(DrawingContext context)
    {
        var snapshot = (Messages ?? []).Take(12).Select(m => new MessageSnapshot(m.Role ?? "", m.Content ?? "")).ToArray();
        context.Custom(new DrawOperation(
            new Rect(Bounds.Size),
            snapshot,
            HasActiveClarification,
            ClarificationTitle ?? "",
            _hoveredBubble,
            SelectedMessageIndex >= 0 ? SelectedMessageIndex : _selectedBubble,
            _bubbleHits));
        base.Render(context);
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
                return i;
        }

        return -1;
    }

    private sealed record MessageSnapshot(string Role, string Content);

    private sealed record BubbleHit(Rect Bounds, int Index);

    private sealed class DrawOperation : ICustomDrawOperation
    {
        private readonly MessageSnapshot[] _messages;
        private readonly bool _hasClarification;
        private readonly string _clarificationTitle;
        private readonly int _hoveredBubble;
        private readonly int _selectedBubble;
        private readonly List<BubbleHit> _hitTargetSink;

        public DrawOperation(
            Rect bounds,
            MessageSnapshot[] messages,
            bool hasClarification,
            string clarificationTitle,
            int hoveredBubble,
            int selectedBubble,
            List<BubbleHit> hitTargetSink)
        {
            Bounds = bounds;
            _messages = messages;
            _hasClarification = hasClarification;
            _clarificationTitle = clarificationTitle;
            _hoveredBubble = hoveredBubble;
            _selectedBubble = selectedBubble;
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
            canvas.Clear(new SKColor(22, 24, 30));

            var width = Math.Max(120f, (float)Bounds.Width);
            var x = 12f;
            var y = 12f;
            var contentWidth = width - 24f;

            using var titlePaint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(214, 224, 244),
                TextSize = 14,
                Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
            };
            canvas.DrawText("Skia Chat Surface (spike)", x, y + 14, titlePaint);
            y += 26;

            if (_hasClarification)
            {
                using var cardPaint = new SKPaint { Color = new SKColor(34, 45, 66), IsAntialias = true };
                using var borderPaint = new SKPaint { Color = new SKColor(88, 120, 178), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
                var rect = new SKRect(x, y, x + contentWidth, y + 44);
                canvas.DrawRoundRect(rect, 8, 8, cardPaint);
                canvas.DrawRoundRect(rect, 8, 8, borderPaint);
                using var txt = new SKPaint
                {
                    IsAntialias = true,
                    Color = new SKColor(201, 218, 255),
                    TextSize = 12,
                    Typeface = SKTypeface.FromFamilyName("Segoe UI")
                };
                var label = string.IsNullOrWhiteSpace(_clarificationTitle) ? "Clarification batch is active" : _clarificationTitle;
                canvas.DrawText(Trim(label, 78), x + 10, y + 26, txt);
                y += 54;
            }

            using var userBg = new SKPaint { Color = new SKColor(53, 72, 112), IsAntialias = true };
            using var assistantBg = new SKPaint { Color = new SKColor(44, 47, 55), IsAntialias = true };
            using var stroke = new SKPaint { Color = new SKColor(84, 92, 108), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            using var hoverStroke = new SKPaint { Color = new SKColor(126, 196, 255), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var selectedStroke = new SKPaint { Color = new SKColor(196, 146, 255), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2.2f };
            using var rolePaint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(181, 196, 230),
                TextSize = 10,
                Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
            };
            using var contentPaint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(223, 228, 236),
                TextSize = 12,
                Typeface = SKTypeface.FromFamilyName("Consolas")
            };

            _hitTargetSink.Clear();
            for (var index = 0; index < _messages.Length; index++)
            {
                var msg = _messages[index];
                var lines = Wrap(Trim(msg.Content, 280), 64);
                var lineCount = Math.Max(1, lines.Count);
                var boxHeight = 22 + lineCount * 16 + 8;
                var r = new SKRect(x, y, x + contentWidth, y + boxHeight);
                var bg = string.Equals(msg.Role, "user", StringComparison.OrdinalIgnoreCase) ? userBg : assistantBg;
                canvas.DrawRoundRect(r, 7, 7, bg);
                canvas.DrawRoundRect(r, 7, 7, stroke);
                if (_hoveredBubble == index)
                    canvas.DrawRoundRect(r, 7, 7, hoverStroke);
                if (_selectedBubble == index)
                    canvas.DrawRoundRect(r, 7, 7, selectedStroke);
                canvas.DrawText(msg.Role.ToUpperInvariant(), x + 10, y + 14, rolePaint);
                var textY = y + 30;
                foreach (var line in lines)
                {
                    canvas.DrawText(line, x + 10, textY, contentPaint);
                    textY += 16;
                }

                _hitTargetSink.Add(new BubbleHit(new Rect(r.Left, r.Top, r.Width, r.Height), index));
                y += boxHeight + 8;
                if (y > Bounds.Height - 24)
                    break;
            }

            canvas.Restore();
        }

        public bool HitTest(Point p) => Bounds.Contains(p);

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose()
        {
        }

        private static string Trim(string text, int maxLen) =>
            text.Length <= maxLen ? text : text[..maxLen] + "...";

        private static List<string> Wrap(string text, int maxChars)
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
    }
}
