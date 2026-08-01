#nullable enable
// Adapted from AvaloniaUI/AvaloniaEdit AvaloniaEdit.TextMate (MIT) for WPF AvalonEdit.
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using TextMateSharp.Themes;
using TmFontStyle = TextMateSharp.Themes.FontStyle;

namespace CDP.GlassCockpit.Windows.TextMate;

abstract class TextTransformation : TextSegment
{
    public abstract void Transform(GenericLineTransformer transformer, DocumentLine line);
}

sealed class ForegroundTextTransformation : TextTransformation
{
    public Dictionary<int, Brush>? ColorMap { get; set; }
    public Action<Exception>? ExceptionHandler { get; set; }
    public int ForegroundColor { get; set; }
    public int BackgroundColor { get; set; }
    public TmFontStyle FontStyle { get; set; }

    public override void Transform(GenericLineTransformer transformer, DocumentLine line)
    {
        try
        {
            if (Length == 0)
                return;

            var formattedOffset = 0;
            var endOffset = line.EndOffset;
            if (StartOffset > line.Offset)
                formattedOffset = StartOffset - line.Offset;
            if (EndOffset < line.EndOffset)
                endOffset = EndOffset;

            transformer.SetTextStyle(
                line,
                formattedOffset,
                endOffset - line.Offset - formattedOffset,
                GetBrush(ForegroundColor),
                GetBrush(BackgroundColor),
                GetFontStyle(),
                GetFontWeight(),
                IsUnderline());
        }
        catch (Exception ex)
        {
            ExceptionHandler?.Invoke(ex);
        }
    }

    System.Windows.FontStyle GetFontStyle()
    {
        if (FontStyle != TmFontStyle.NotSet && (FontStyle & TmFontStyle.Italic) != 0)
            return FontStyles.Italic;
        return FontStyles.Normal;
    }

    FontWeight GetFontWeight()
    {
        if (FontStyle != TmFontStyle.NotSet && (FontStyle & TmFontStyle.Bold) != 0)
            return FontWeights.Bold;
        return FontWeights.Normal;
    }

    bool IsUnderline() =>
        FontStyle != TmFontStyle.NotSet && (FontStyle & TmFontStyle.Underline) != 0;

    Brush? GetBrush(int colorId)
    {
        if (ColorMap is null)
            return null;
        return ColorMap.TryGetValue(colorId, out var brush) ? brush : null;
    }
}
