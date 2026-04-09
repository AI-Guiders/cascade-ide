using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: настройки и справка (см. <c>IdeCommands.Chrome.cs</c> — OpenSettings, About).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterSettingsAndHelpPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        // ——— Настройки и справка
        AddPalette(b, "open_settings", IdeCommands.OpenSettings, "Параметры…", "Настройки");
        AddPalette(b, "about", IdeCommands.About, "О программе", "Справка");
    }
}
