#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CascadeIDE.SoftInstrument;
using SkiaSharp;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SemanticMap Skia graph — multi-hop peel from RelatedFiles feed.</summary>
public sealed class GlassSemanticMapSkia : FrameworkElement
{
    readonly List<NodeHit> _hits = new();
    GlassSemanticMapGraph.Graph _graph = new(null, [], []);
    WriteableBitmap? _wb;

    public event Action<string>? NodeActivated;

    public GlassSemanticMapSkia()
    {
        Focusable = true;
        ClipToBounds = true;
        SnapsToDevicePixels = true;
        SizeChanged += (_, _) => InvalidateVisual();
    }

    public void SetGraph(GlassSemanticMapGraph.Graph graph)
    {
        _graph = graph;
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
        var focusR = Math.Min(w, h) * 0.34f;

        using var edgePaint = new SKPaint
        {
            Color = new SKColor(0x55, 0x88, 0xAA, 0xAA),
            StrokeWidth = 1.2f,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
        };
        using var hop2Edge = new SKPaint
        {
            Color = new SKColor(0x44, 0x66, 0x77, 0x88),
            StrokeWidth = 0.9f,
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
        using var hop2Fill = new SKPaint
        {
            Color = new SKColor(0x22, 0x22, 0x22),
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

        var positions = LayoutNodes(cx, cy, focusR, w, h);
        var focusPath = _graph.FocusPath;
        var focusLabel = focusPath is null ? "(no focus)" : Path.GetFileName(focusPath);
        const float focusNodeR = 18f;
        canvas.DrawCircle(cx, cy, focusNodeR, focusFill);
        canvas.DrawCircle(cx, cy, focusNodeR, nodeStroke);
        canvas.DrawText(Trim(focusLabel, 22), cx - 40, cy + focusNodeR + 14, font, textPaint);
        if (focusPath is not null)
            _hits.Add(new NodeHit(focusPath, cx, cy, focusNodeR + 4));

        foreach (var edge in _graph.Edges)
        {
            if (!positions.TryGetValue(edge.FromPath, out var a) || !positions.TryGetValue(edge.ToPath, out var b))
                continue;
            var hop = _graph.Nodes.FirstOrDefault(n => string.Equals(n.FilePath, edge.ToPath, StringComparison.OrdinalIgnoreCase))?.Hop ?? 1;
            canvas.DrawLine(a.X, a.Y, b.X, b.Y, hop >= 2 ? hop2Edge : edgePaint);
        }

        foreach (var node in _graph.Nodes.Take(48))
        {
            if (!positions.TryGetValue(node.FilePath, out var pos))
                continue;

            var nodeR = node.Hop >= 2 ? 8f : ReasonRadius(node.Rationale);
            var fill = node.Hop >= 2 ? hop2Fill : nodeFill;
            canvas.DrawCircle(pos.X, pos.Y, nodeR, fill);
            canvas.DrawCircle(pos.X, pos.Y, nodeR, nodeStroke);

            var label = Trim(Path.GetFileName(node.FilePath), 18);
            canvas.DrawText(label, pos.X + nodeR + 3, pos.Y + 4, font, textPaint);
            _hits.Add(new NodeHit(node.FilePath, pos.X, pos.Y, nodeR + 6));
        }

        if (_graph.Nodes.Count == 0)
            canvas.DrawText("semantic · empty — open a file / refresh", 12, 20, font, textPaint);
    }

    Dictionary<string, SKPoint> LayoutNodes(float cx, float cy, float radius, int w, int h)
    {
        var map = new Dictionary<string, SKPoint>(StringComparer.OrdinalIgnoreCase);
        var hop1 = _graph.Nodes.Where(n => n.Hop == 1).ToList();
        var hop2 = _graph.Nodes.Where(n => n.Hop >= 2).ToList();

        var n1 = hop1.Count;
        for (var i = 0; i < n1; i++)
        {
            var angle = (float)(-Math.PI / 2 + 2 * Math.PI * i / Math.Max(1, n1));
            var x = cx + radius * MathF.Cos(angle);
            var y = cy + radius * MathF.Sin(angle);
            map[hop1[i].FilePath] = new SKPoint(x, y);
        }

        var outer = Math.Min(w, h) * 0.44f;
        var n2 = hop2.Count;
        for (var i = 0; i < n2; i++)
        {
            var angle = (float)(-Math.PI / 2 + 2 * Math.PI * i / Math.Max(1, n2) + Math.PI / Math.Max(1, n2));
            var x = cx + outer * MathF.Cos(angle);
            var y = cy + outer * MathF.Sin(angle);
            map[hop2[i].FilePath] = new SKPoint(x, y);
        }

        return map;
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
