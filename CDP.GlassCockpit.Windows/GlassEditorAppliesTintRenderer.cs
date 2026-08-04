#nullable enable

using System.Windows;
using System.Windows.Media;
using CascadeIDE.SoftOrgan;
using ICSharpCode.AvalonEdit.Rendering;

namespace CDP.GlassCockpit.Windows;

/// <summary>AvalonEdit tint for Applies-on-locus error/warn lines (amber/red).</summary>
internal sealed class GlassEditorAppliesTintRenderer : IBackgroundRenderer
{
    static readonly Brush ErrBg = Freeze(Color.FromArgb(0x55, 0x3A, 0x1A, 0x1A));
    static readonly Brush WarnBg = Freeze(Color.FromArgb(0x44, 0x3A, 0x32, 0x14));

    HashSet<int> _errs = [];
    HashSet<int> _warns = [];

    public KnownLayer Layer => KnownLayer.Background;

    public void Apply(GlassEditorAppliesLocus.Face? face)
    {
        if (face is null || !face.HasTint)
        {
            _errs = [];
            _warns = [];
            return;
        }

        _errs = face.ErrorLines.ToHashSet();
        _warns = face.WarnLines.ToHashSet();
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document is null || (_errs.Count == 0 && _warns.Count == 0))
            return;

        foreach (var vline in textView.VisualLines)
        {
            var line = vline.FirstDocumentLine.LineNumber;
            Brush? brush = null;
            if (_errs.Contains(line))
                brush = ErrBg;
            else if (_warns.Contains(line))
                brush = WarnBg;
            if (brush is null)
                continue;

            var y = vline.VisualTop - textView.ScrollOffset.Y;
            drawingContext.DrawRectangle(
                brush,
                null,
                new Rect(0, y, textView.ActualWidth, vline.Height));
        }
    }

    static SolidColorBrush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
