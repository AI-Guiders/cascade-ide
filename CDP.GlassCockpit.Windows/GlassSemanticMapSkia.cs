#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CascadeIDE.SoftOrgan;
using SkiaSharp;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SemanticMap Skia graph — radial peel from RelatedFiles heuristic (no Avalonia WNM fork).</summary>
public sealed class GlassSemanticMapSkia : FrameworkElement
{
    readonly List<NodeHit> _hits = new();
    IReadOnlyList<GlassRelatedFilesHeuristic.Item> _items = Array.Empty<GlassRelatedFilesHeuristic.Item>();
    string? _focusPath;
    WriteableBitmap? _wb;

    public event Action<string>? NodeActivated;

    public GlassSemanticMapSkia()
    {
        Focusable = true;
        ClipToBounds = true;
        SnapsToDevicePixels = true;
        SizeChanged += (_, _) => InvalidateVisual();
    }

    public void SetGraph(string? focusPath, IReadOnlyList<GlassRelatedFilesHeuristic.Item> items)
    {
        _focusPath = string.IsNullOrWhiteSpace(focusPath) ? null : focusPath;
        _items = items;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var p = e.GetPosition(this);
        var hit = HitTest((float)p.X, (float)p.Y);
        if (hit is null)
            return;
        e.Handled = true;
        NodeActivated?.Invoke(hit.FilePath);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var w = (int)Math.Ceiling(Math.Max(1, ActualWidth));
        var h = (int)Math.Ceiling(Math.Max(1, ActualHeight));
        if (w < 8 || h < 8)
            return;

        var dpi = VisualTreeHelper.GetDpi(this);
        var pixelW = Math.Max(1, (int)Math.Ceiling(w * dpi.DpiScaleX));
        var pixelH = Math.Max(1, (int)Math.Ceiling(h * dpi.DpiScaleY));

        if (_wb is null || _wb.PixelWidth != pixelW || _wb.PixelHeight != pixelH)
        {
            _wb = new WriteableBitmap(
                pixelW,
                pixelH,
                dpi.PixelsPerInchX,
                dpi.PixelsPerInchY,
                PixelFormats.Bgra32,
                null);
        }

        _wb.Lock();
        try
        {
            using var surface = SKSurface.Create(
                new SKImageInfo(pixelW, pixelH, SKColorType.Bgra8888, SKAlphaType.Premul),
                _wb.BackBuffer,
                _wb.BackBufferStride);
            var canvas = surface.Canvas;
            canvas.Clear(new SKColor(0x12, 0x12, 0x12));
            canvas.Scale((float)dpi.DpiScaleX, (float)dpi.DpiScaleY);
            Paint(canvas, w, h);
            surface.Flush();
            _wb.AddDirtyRect(new Int32Rect(0, 0, pixelW, pixelH));
        }
        finally
        {
            _wb.Unlock();
        }

        drawingContext.DrawImage(_wb, new Rect(0, 0, w, h));
    }

    void Paint(SKCanvas canvas, int w, int h)
    {
        _hits.Clear();
        var cx = w * 0.5f;
        var cy = h * 0.5f;
        var radius = Math.Min(w, h) * 0.36f;

        using var edgePaint = new SKPaint
        {
            Color = new SKColor(0x55, 0x88, 0xAA, 0xAA),
            StrokeWidth = 1.2f,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
        };
        using var focusFill = new SKPaint
        {
            Color = new SKColor(0x3D, 0x8B, 0xF0),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        using var nodeFill = new SKPaint
        {
            Color = new SKColor(0x2A, 0x2A, 0x2A),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        using var nodeStroke = new SKPaint
        {
            Color = new SKColor(0x6A, 0x6A, 0x6A),
            StrokeWidth = 1f,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
        };
        using var textPaint = new SKPaint
        {
            Color = new SKColor(0xDD, 0xDD, 0xDD),
            IsAntialias = true,
        };
        using var font = new SKFont(SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal), 11);

        var focusLabel = _focusPath is null ? "(no focus)" : Path.GetFileName(_focusPath);
        const float focusR = 18f;
        canvas.DrawCircle(cx, cy, focusR, focusFill);
        canvas.DrawCircle(cx, cy, focusR, nodeStroke);
        canvas.DrawText(Trim(focusLabel, 22), cx - 40, cy + focusR + 14, font, textPaint);
        if (_focusPath is not null)
            _hits.Add(new NodeHit(_focusPath, cx, cy, focusR + 4));

        var n = Math.Min(_items.Count, 48);
        if (n == 0)
        {
            canvas.DrawText("semantic · empty — open a file / refresh", 12, 20, font, textPaint);
            return;
        }

        for (var i = 0; i < n; i++)
        {
            var item = _items[i];
            var angle = (float)(-Math.PI / 2 + 2 * Math.PI * i / n);
            var x = cx + radius * MathF.Cos(angle);
            var y = cy + radius * MathF.Sin(angle);
            var nodeR = ReasonRadius(item.Reason);

            canvas.DrawLine(cx, cy, x, y, edgePaint);
            canvas.DrawCircle(x, y, nodeR, nodeFill);
            canvas.DrawCircle(x, y, nodeR, nodeStroke);

            var label = Trim(Path.GetFileName(item.FilePath), 18);
            canvas.DrawText(label, x + nodeR + 3, y + 4, font, textPaint);
            _hits.Add(new NodeHit(item.FilePath, x, y, nodeR + 6));
        }
    }

    NodeHit? HitTest(float x, float y)
    {
        NodeHit? best = null;
        var bestD = float.MaxValue;
        foreach (var hit in _hits)
        {
            var dx = x - hit.X;
            var dy = y - hit.Y;
            var d = MathF.Sqrt(dx * dx + dy * dy);
            if (d <= hit.Radius && d < bestD)
            {
                best = hit;
                bestD = d;
            }
        }

        return best;
    }

    static float ReasonRadius(string reason) =>
        reason.Contains("stem", StringComparison.OrdinalIgnoreCase) ? 12f :
        reason.Contains("md", StringComparison.OrdinalIgnoreCase) ? 10f : 9f;

    static string Trim(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";

    sealed record NodeHit(string FilePath, float X, float Y, float Radius);
}
