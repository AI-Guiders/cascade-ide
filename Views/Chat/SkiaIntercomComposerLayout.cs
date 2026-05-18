#nullable enable
using CascadeIDE.Views.SkiaKit;

namespace CascadeIDE.Views.Chat;

/// <summary>Метрики нижнего chrome Intercom (composer + slash popup) для layout и тестов.</summary>
internal static class SkiaIntercomComposerLayout
{
    public static float MeasureBottomChromeHeight(
        bool showComposer,
        bool showSlashPopup,
        int slashRowCount,
        string composerText,
        float surfaceWidth)
    {
        if (!showComposer)
            return 0f;

        var contentWidth = Math.Max(40f, surfaceWidth - SkiaComposerStrip.HorizontalPadding * 2 - SkiaComposerStrip.SendButtonWidth - 24f);
        var composer = SkiaComposerStrip.MeasureHeight(composerText, preeditText: null, contentWidth);
        var popup = showSlashPopup ? SkiaPopupList.MeasureHeight(slashRowCount) + 4f : 0f;
        return composer + popup;
    }
}
