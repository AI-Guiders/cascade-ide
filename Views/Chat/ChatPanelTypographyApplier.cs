#nullable enable

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using CascadeIDE.Models;

namespace CascadeIDE.Views.Chat;

/// <summary>Применяет <c>[fonts.intercom]</c> к Avalonia-элементам MFD Chat (заголовки, spine, уточнения).</summary>
internal static class ChatPanelTypographyApplier
{
    public const string TitleClass = "chatPanelTitle";
    public const string SubtitleClass = "chatPanelSubtitle";
    public const string LabelClass = "chatPanelLabel";
    public const string BodyClass = "chatPanelBody";
    public const string InputClass = "chatPanelInput";

    public static void Apply(Control root, IntercomFontsSettings fonts)
    {
        var family = new FontFamily(fonts.ResolveProseFamily());
        var titlePt = fonts.ResolvePanelTitlePt();
        var subtitlePt = fonts.ResolvePanelSubtitlePt();
        var labelPt = fonts.ResolvePanelLabelPt();
        var bodyPt = fonts.ResolvePanelBodyPt();
        var inputPt = fonts.ResolvePanelInputPt();

        foreach (var control in root.GetVisualDescendants().OfType<Control>())
        {
            if (control is TextBlock tb)
                ApplyTextBlock(tb, family, titlePt, subtitlePt, labelPt, bodyPt);
            else if (control is TextBox box)
                ApplyTextBox(box, family, inputPt);
            else if (control is ComboBox combo && control.Classes.Contains(InputClass))
                ApplyComboBox(combo, family, inputPt);
            else if (control is Expander expander && control.Classes.Contains(LabelClass))
            {
                expander.FontFamily = family;
                expander.FontSize = labelPt;
            }
        }
    }

    private static void ApplyTextBlock(
        TextBlock tb,
        FontFamily family,
        float titlePt,
        float subtitlePt,
        float labelPt,
        float bodyPt)
    {
        tb.FontFamily = family;
        if (tb.Classes.Contains(TitleClass))
            tb.FontSize = titlePt;
        else if (tb.Classes.Contains(SubtitleClass))
            tb.FontSize = subtitlePt;
        else if (tb.Classes.Contains(LabelClass))
            tb.FontSize = labelPt;
        else if (tb.Classes.Contains(BodyClass))
            tb.FontSize = bodyPt;
    }

    private static void ApplyTextBox(TextBox box, FontFamily family, float inputPt)
    {
        if (!box.Classes.Contains(InputClass))
            return;

        box.FontFamily = family;
        box.FontSize = inputPt;
        var scale = inputPt / 12f;
        if (box.MinHeight > 0 && box.MinHeight < 200)
            box.MinHeight = Math.Max(box.MinHeight, 32f * scale);
    }

    private static void ApplyComboBox(ComboBox combo, FontFamily family, float inputPt)
    {
        combo.FontFamily = family;
        combo.FontSize = inputPt;
    }
}
