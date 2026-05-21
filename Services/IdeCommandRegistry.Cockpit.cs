using System.Collections.Immutable;

namespace CascadeIDE.Services;

public static partial class IdeCommandRegistry
{
    private static void RegisterCockpitPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        AddPalette(b, "cockpit_open_command_line", IdeCommands.CockpitOpenCommandLine,
            "Cockpit: Command Line (slash REPL)", "Cockpit");
    }
}
