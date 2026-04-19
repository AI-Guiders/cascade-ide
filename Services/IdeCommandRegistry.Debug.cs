using System.Collections.Immutable;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Services;

/// <summary>Палитра и хоткеи окна: отладка (см. <c>IdeCommands.DebuggerUi.cs</c>, <c>IdeCommands.DapDebug.cs</c>).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterDebugPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        // ——— Отладка — в палитре только при семействе Debug.
        AddPalette(b, "debug_continue", IdeCommands.DebugContinue, "Отладка: продолжить", "Отладка", null, ImmutableArray.Create(UiModeFamily.Debug));
        AddPalette(
            b,
            "debug_step_over",
            IdeCommands.DebugStepOver,
            "Отладка: шаг с обходом",
            "Отладка",
            null,
            ImmutableArray.Create(UiModeFamily.Debug),
            CommandAccessibleFrom.AgentAndUI,
            new MainWindowHotkeyVmBinding(MainWindowHotkeyVmBindingKind.DebugStepOver));
        AddPalette(
            b,
            "debug_step_into",
            IdeCommands.DebugStepInto,
            "Отладка: шаг с заходом",
            "Отладка",
            null,
            ImmutableArray.Create(UiModeFamily.Debug),
            CommandAccessibleFrom.AgentAndUI,
            new MainWindowHotkeyVmBinding(MainWindowHotkeyVmBindingKind.DebugStepInto));
        AddPalette(
            b,
            "debug_step_out",
            IdeCommands.DebugStepOut,
            "Отладка: шаг с выходом",
            "Отладка",
            null,
            ImmutableArray.Create(UiModeFamily.Debug),
            CommandAccessibleFrom.AgentAndUI,
            new MainWindowHotkeyVmBinding(MainWindowHotkeyVmBindingKind.DebugStepOut));
        AddPalette(
            b,
            "debug_stop",
            IdeCommands.DebugStop,
            "Отладка: остановить",
            "Отладка",
            null,
            ImmutableArray.Create(UiModeFamily.Debug),
            CommandAccessibleFrom.AgentAndUI,
            new MainWindowHotkeyVmBinding(MainWindowHotkeyVmBindingKind.DebugStop));
        AddPalette(b, "debug_ping", IdeCommands.DebugPing, "Отладка: ping", "Отладка", null, ImmutableArray.Create(UiModeFamily.Debug));
    }
}
