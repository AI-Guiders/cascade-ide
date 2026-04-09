using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services;

/// <summary>
/// Жесты главного окна из merged <c>hotkeys.toml</c>: <see cref="Window.KeyBindings"/>, подсказки в меню,
/// tunnel при фокусе в редакторе. Источник привязок id → VM — <see cref="IdeCommandRegistry"/>.
/// </summary>
public static class MainWindowHotkeyService
{
    /// <summary>Алиас для ключа TOML/UI-only старта отладки (см. <see cref="IdeCommandRegistry.DebugStartOrContinueHotkeyId"/>).</summary>
    public const string DebugStartOrContinueId = IdeCommandRegistry.DebugStartOrContinueHotkeyId;

    private static IReadOnlyDictionary<string, string>? _mergedMap;

    public static IReadOnlyDictionary<string, string> GetMergedMap() =>
        _mergedMap ??= HotkeyTomlLoader.LoadMergedDictionary();

    public static void ApplyAll(Window window, MainWindowViewModel vm)
    {
        var map = HotkeyTomlLoader.LoadMergedDictionary();
        _mergedMap = map;
        ApplyWindowKeyBindings(window, vm, map);
        ApplyMenuHotKeys(window, map);
    }

    private static void ApplyWindowKeyBindings(
        Window window,
        MainWindowViewModel vm,
        IReadOnlyDictionary<string, string> map)
    {
        window.KeyBindings.Clear();
        foreach (var e in IdeCommandRegistry.AllEntries)
        {
            if (e.WindowHotkey is not { } wh)
                continue;
            if (!map.TryGetValue(e.EffectiveHotkeysTomlKey, out var s) || string.IsNullOrWhiteSpace(s))
                continue;
            var (cmd, param) = ResolveWindowCommand(vm, wh);
            TryAddKeyBinding(window, s, cmd, param);
        }
    }

    private static void TryAddKeyBinding(Window window, string gestureString, ICommand command, object? parameter)
    {
        try
        {
            var g = KeyGesture.Parse(gestureString.Trim());
            var kb = new KeyBinding { Gesture = g, Command = command };
            if (parameter is not null)
                kb.CommandParameter = parameter;
            window.KeyBindings.Add(kb);
        }
        catch
        {
            // неверная строка жеста в TOML — пропускаем
        }
    }

    private static (ICommand Command, object? Parameter) ResolveWindowCommand(MainWindowViewModel vm, IdeCommandRegistry.MainWindowHotkeyVmBinding b) =>
        b.Kind switch
        {
            IdeCommandRegistry.MainWindowHotkeyVmBindingKind.ToggleCommandPalette => (vm.ToggleCommandPaletteCommand, null),
            IdeCommandRegistry.MainWindowHotkeyVmBindingKind.CycleUiMode => (vm.CycleUiModeCommand, null),
            IdeCommandRegistry.MainWindowHotkeyVmBindingKind.SetUiModeByIndex => (vm.SetUiModeByIndexCommand, b.SetUiModeIndex),
            IdeCommandRegistry.MainWindowHotkeyVmBindingKind.DebugStartOrContinue => (vm.DebugStartOrContinueCommand, null),
            IdeCommandRegistry.MainWindowHotkeyVmBindingKind.DebugStop => (vm.DebugStopCommand, null),
            IdeCommandRegistry.MainWindowHotkeyVmBindingKind.DebugStepOver => (vm.DebugStepOverCommand, null),
            IdeCommandRegistry.MainWindowHotkeyVmBindingKind.DebugStepInto => (vm.DebugStepIntoCommand, null),
            IdeCommandRegistry.MainWindowHotkeyVmBindingKind.DebugStepOut => (vm.DebugStepOutCommand, null),
            _ => throw new ArgumentOutOfRangeException(nameof(b)),
        };

    private static void ApplyMenuHotKeys(Window window, IReadOnlyDictionary<string, string> map)
    {
        SetMenuHotKey(window, "MenuDebugStartOrContinue", map, DebugStartOrContinueId);
        SetMenuHotKey(window, "MenuDebugStop", map, IdeCommands.DebugStop);
        SetMenuHotKey(window, "MenuDebugStepOver", map, IdeCommands.DebugStepOver);
        SetMenuHotKey(window, "MenuDebugStepInto", map, IdeCommands.DebugStepInto);
        SetMenuHotKey(window, "MenuDebugStepOut", map, IdeCommands.DebugStepOut);
        SetMenuHotKey(window, "MenuCommandPalette", map, "toggle_command_palette");
    }

    private static void SetMenuHotKey(Window window, string controlName, IReadOnlyDictionary<string, string> map, string id)
    {
        if (window.FindControl<MenuItem>(controlName) is not { } item)
            return;
        if (!map.TryGetValue(id, out var s) || string.IsNullOrWhiteSpace(s))
        {
            item.HotKey = null;
            return;
        }

        try
        {
            item.HotKey = KeyGesture.Parse(s.Trim());
        }
        catch
        {
            item.HotKey = null;
        }
    }

    /// <summary>
    /// Дублирует <see cref="Window.KeyBindings"/> в tunnel: при фокусе в редакторе жесты до команд не всегда доходят.
    /// </summary>
    /// <returns><see langword="true"/>, если событие обработано (выполнена команда).</returns>
    public static bool TryHandleTunnelShortcuts(KeyEventArgs e, MainWindowViewModel vm)
    {
        var map = GetMergedMap();

        foreach (var tomlKey in IdeCommandRegistry.TunnelShortcutKeyOrder)
        {
            if (!IdeCommandRegistry.WindowHotkeyByTomlKey.TryGetValue(tomlKey, out var entry))
                continue;
            if (entry.WindowHotkey is not { } wh)
                continue;
            if (!map.TryGetValue(tomlKey, out var s) || string.IsNullOrWhiteSpace(s))
                continue;
            KeyGesture g;
            try
            {
                g = KeyGesture.Parse(s.Trim());
            }
            catch
            {
                continue;
            }

            if (!g.Matches(e))
                continue;

            if (tomlKey.Equals("toggle_command_palette", StringComparison.OrdinalIgnoreCase))
            {
                if (vm.IsCommandPaletteOpen)
                    return false;
                if (!vm.ToggleCommandPaletteCommand.CanExecute(null))
                    return false;
                vm.ToggleCommandPaletteCommand.Execute(null);
                e.Handled = true;
                return true;
            }

            var (cmd, param) = ResolveWindowCommand(vm, wh);
            if (!cmd.CanExecute(param))
                return false;
            cmd.Execute(param);
            e.Handled = true;
            return true;
        }

        return false;
    }
}
