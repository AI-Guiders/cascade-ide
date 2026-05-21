using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>Заводская типографика Intercom из <see cref="SettingsDefaultsLoader"/> (для Avalonia default и Skia без VM).</summary>
public static class IntercomFontDefaults
{
    private static IntercomFontsSettings? s_intercom;

    public static IntercomFontsSettings Intercom =>
        s_intercom ??= SettingsDefaultsLoader.CreateDefault().Fonts.Intercom;
}
