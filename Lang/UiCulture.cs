using System.Globalization;
using System.Threading;
using CascadeIDE.Services;

namespace CascadeIDE.Lang;

/// <summary>Инициализация языка UI (паттерн IncomeCascade: <c>Resources.Culture</c> + поток).</summary>
public static class UiCulture
{
    /// <summary>
    /// Если в <c>settings.toml</c> задан непустой <see cref="Models.WorkspaceSettings.Culture"/> — применить его;
    /// иначе как <see cref="ApplyFromSystem"/>.
    /// </summary>
    public static void ApplyFromSettingsOrSystem()
    {
        try
        {
            var name = SettingsService.Load().Workspace.Culture?.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                Apply(CultureInfo.GetCultureInfo(name));
                return;
            }
        }
        catch (CultureNotFoundException)
        {
            // падаем на системную логику
        }

        ApplyFromSystem();
    }

    /// <summary>Выставить культуру до создания главного окна. Русская локаль Windows → ru-RU, иначе en-US (есть спутники в сборке).</summary>
    public static void ApplyFromSystem()
    {
        var culture = ResolveCulture(CultureInfo.CurrentUICulture);
        Apply(culture);
    }

    /// <summary>Явная культура (меню «Вид → Язык», настройки позже).</summary>
    public static void Apply(CultureInfo culture)
    {
        if (LocViewModel.Current is { } loc)
            loc.SetCulture(culture);
        else
        {
            Resources.Culture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
    }

    static CultureInfo ResolveCulture(CultureInfo ui)
    {
        try
        {
            if (ui.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
                return CultureInfo.GetCultureInfo("ru-RU");
            return CultureInfo.GetCultureInfo("en-US");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("en-US");
        }
    }
}
