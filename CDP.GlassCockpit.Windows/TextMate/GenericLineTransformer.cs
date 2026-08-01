#nullable enable
// Adapted from AvaloniaUI/AvaloniaEdit AvaloniaEdit.TextMate (MIT) for WPF AvalonEdit.
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace CDP.GlassCockpit.Windows.TextMate;

abstract class GenericLineTransformer : DocumentColorizingTransformer
{
    readonly Action<Exception>? _exceptionHandler;

    protected GenericLineTransformer(Action<Exception>? exceptionHandler) =>
        _exceptionHandler = exceptionHandler;

    protected override void ColorizeLine(DocumentLine line)
    {
        try
        {
            TransformLine(line, CurrentContext);
        }
        catch (Exception ex)
        {
            _exceptionHandler?.Invoke(ex);
        }
    }

    protected abstract void TransformLine(DocumentLine line, ITextRunConstructionContext context);

    public void SetTextStyle(
        DocumentLine line,
        int startIndex,
        int length,
        Brush? foreground,
        Brush? background,
        FontStyle fontStyle,
        FontWeight fontWeight,
        bool isUnderline)
    {
        int startOffset;
        int endOffset;
        if (startIndex >= 0 && length > 0)
        {
            if (line.Offset + startIndex + length > line.EndOffset)
                length = line.EndOffset - line.Offset - startIndex;
            startOffset = line.Offset + startIndex;
            endOffset = line.Offset + startIndex + length;
        }
        else
        {
            startOffset = line.Offset;
            endOffset = line.EndOffset;
        }

        if (startOffset > CurrentContext.Document.TextLength ||
            endOffset > CurrentContext.Document.TextLength)
            return;

        ChangeLinePart(
            startOffset,
            endOffset,
            visualLine => ChangeVisualLine(visualLine, foreground, background, fontStyle, fontWeight, isUnderline));
    }

    static void ChangeVisualLine(
        VisualLineElement visualLine,
        Brush? foreground,
        Brush? background,
        FontStyle fontStyle,
        FontWeight fontWeight,
        bool isUnderline)
    {
        if (foreground is not null)
            visualLine.TextRunProperties.SetForegroundBrush(foreground);
        if (background is not null)
            visualLine.TextRunProperties.SetBackgroundBrush(background);
        if (isUnderline)
            visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Underline);

        var tf = visualLine.TextRunProperties.Typeface;
        if (tf.Style != fontStyle || tf.Weight != fontWeight)
        {
            visualLine.TextRunProperties.SetTypeface(
                new Typeface(tf.FontFamily, fontStyle, fontWeight, tf.Stretch));
        }
    }
}
