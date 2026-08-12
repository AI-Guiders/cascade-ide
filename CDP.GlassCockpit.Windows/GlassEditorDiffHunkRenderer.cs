#nullable enable

using System.Windows;
using System.Windows.Media;
using CascadeIDE.SoftInstrument;
using ICSharpCode.AvalonEdit.Rendering;

namespace CDP.GlassCockpit.Windows;

/// <summary>AvalonEdit background tint for Diff-intent add lines (+ delete anchors).</summary>
internal sealed class GlassEditorDiffHunkRenderer : IBackgroundRenderer
{
    static readonly Brush AddBg = Freeze(Color.FromArgb(0x55, 0x1A, 0x3A, 0x1A));
    static readonly Brush DelBg = Freeze(Color.FromArgb(0x44, 0x3A, 0x1A, 0x1A));

    HashSet<int> _adds = [];
    HashSet<int> _dels = [];

    public KnownLayer Layer => KnownLayer.Background;

    public void Apply(GlassEditorDiffIntent.Face? face)
    {
        if (face is null || !face.HasTint)
        {
            _adds = [];
            _dels = [];
            return;
        }

        _adds = face.AddLines.ToHashSet();
        _dels = face.DeleteAnchors.ToHashSet();
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document is null || (_adds.Count == 0 && _dels.Count == 0))
            return;

        foreach (var vline in textView.VisualLines)
        {
            var line = vline.FirstDocumentLine.LineNumber;
            Brush? brush = null;
            if (_adds.Contains(line))
                brush = AddBg;
            else if (_dels.Contains(line))
                brush = DelBg;
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
