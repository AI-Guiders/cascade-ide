#nullable enable
namespace CascadeIDE.Views.Chat;

/// <summary>Метрики нижнего chrome Intercom (Command Deck) для layout и тестов.</summary>
internal static class SkiaIntercomComposerLayout
{
    public static float MeasureBottomChromeHeight(
        bool showComposer,
        bool showSlashPopup,
        int slashRowCount,
        string composerText,
        float surfaceWidth,
        bool showCommandLine = false,
        string? commandLinePreview = null,
        string? composerSlashPreview = null) =>
        SkiaIntercomCommandDeckLayout.MeasureTotalHeight(
            surfaceWidth,
            showComposer,
            showCommandLine,
            commandLinePreview,
            composerText,
            showSlashPopup,
            slashRowCount,
            composerSlashPreview: composerSlashPreview);
}
