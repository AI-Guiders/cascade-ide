using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Записи только для глобальных хоткеев окна (без строки палитры / без MCP id где указано).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterWindowOnlyHotkeys(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        // ——— Режим по индексу в каталоге — только хоткеи окна (не MCP-строка палитры).
        for (var i = 0; i <= SetUiModeByIndexMaxInclusive; i++)
        {
            AddWindowOnly(
                b,
                $"set_ui_mode_by_index_{i}",
                commandId: null,
                new MainWindowHotkeyVmBinding(MainWindowHotkeyVmBindingKind.SetUiModeByIndex, i),
                CommandAccessibleFrom.UIOnly);
        }

        // ——— F5 старт/продолжить — только UI (id = DebugStartOrContinueHotkeyId).
        AddWindowOnly(
            b,
            DebugStartOrContinueHotkeyId,
            commandId: null,
            new MainWindowHotkeyVmBinding(MainWindowHotkeyVmBindingKind.DebugStartOrContinue),
            CommandAccessibleFrom.UIOnly);
    }
}
