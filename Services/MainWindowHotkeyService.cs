using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services;

/// <summary>
/// Жесты главного окна из merged <c>hotkeys.toml</c>: подсказки в меню,
/// tunnel <see cref="InputElement.KeyDownEvent"/> на главном окне (см. <see cref="TryHandleTunnelShortcuts"/>).
/// Глобальные команды не дублируем через <see cref="Window.KeyBindings"/>: в Avalonia обработка KeyBinding идёт до
/// <c>RaiseEvent</c> и при успехе ставит <see cref="RoutedEventArgs.Handled"/> — туннельный обработчик на окне
/// с <c>handledEventsToo: false</c> тогда не вызывается, а строгий <see cref="KeyGesture.Matches"/> не совпадает
/// с <see cref="KeyGestureChordMatching"/> (модификаторы, раскладка).
/// Источник привязок id → VM — <see cref="IdeCommandRegistry"/>.
/// </summary>
public static class MainWindowHotkeyService
{
    /// <summary>Алиас для ключа TOML/UI-only старта отладки (см. <see cref="IdeCommandRegistry.DebugStartOrContinueHotkeyId"/>).</summary>
    public const string DebugStartOrContinueId = IdeCommandRegistry.DebugStartOrContinueHotkeyId;

    private static IReadOnlyDictionary<string, string>? _mergedMap;
    private static readonly object HotkeyLogLock = new();

    /// <summary>Сброс кэша merged TOML (только тесты: изоляция и подмена файлов в <see cref="AppContext.BaseDirectory"/>).</summary>
    internal static void ClearMergedHotkeyMapCacheForTests() => _mergedMap = null;

    /// <summary>
    /// Только тесты: подменить merged map без диска (изоляция от <c>%LocalAppData%\CascadeIDE\hotkeys.toml</c>).
    /// <see langword="null"/> — сброс, следующий <see cref="GetMergedMap"/> загрузит с диска.
    /// </summary>
    internal static void ReplaceMergedMapForTests(IReadOnlyDictionary<string, string>? map) => _mergedMap = map;

    public static IReadOnlyDictionary<string, string> GetMergedMap() =>
        _mergedMap ??= HotkeyTomlLoader.LoadMergedDictionary();

    internal static void LogTunnelEvent(string source, KeyEventArgs e, MainWindowViewModel vm, string stage)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, ".cascade-ide");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "hotkey-log.txt");
            var line =
                $"[{DateTimeOffset.Now:O}] source={source} stage={stage} key={e.Key} physical={e.PhysicalKey} mods={e.KeyModifiers} handled={e.Handled} paletteOpen={vm.IsCommandPaletteOpen}{Environment.NewLine}";
            lock (HotkeyLogLock)
                File.AppendAllText(path, line);
        }
        catch
        {
            // Never break input processing because of debug logging.
        }
    }

    public static void ApplyAll(Window window, MainWindowViewModel vm)
    {
        var map = HotkeyTomlLoader.LoadMergedDictionary();
        _mergedMap = map;
        ApplyWindowKeyBindings(window);
        ApplyMenuHotKeys(window, map);
    }

    private static void ApplyWindowKeyBindings(Window window) => window.KeyBindings.Clear();

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

    /// <summary>
    /// Tunnel KeyDown для любого <see cref="Window"/> с тем же <see cref="MainWindowViewModel"/> (главное окно, PFD/MFD-хосты):
    /// CascadeChord → Esc закрывает палитру (путь туннеля до фокуса в редакторе не проходит через <c>CommandPaletteView</c>) → жесты из TOML.
    /// </summary>
    public static bool TryHandleTunnelKeyDownForMainVm(KeyEventArgs e, MainWindowViewModel vm)
    {
        if (vm.TryConsumeCascadeChordKeyDown(e))
        {
            LogTunnelEvent("HotkeyService", e, vm, "cascade-consumed");
            return true;
        }

        if (vm.IsCommandPaletteOpen
            && e.Key == Key.Escape
            && KeyGestureChordMatching.NormalizeChordModifiers(e.KeyModifiers) == KeyModifiers.None)
        {
            if (!vm.CloseCommandPaletteCommand.CanExecute(null))
            {
                LogTunnelEvent("HotkeyService", e, vm, "palette-esc-cannot-execute");
                return false;
            }
            vm.CloseCommandPaletteCommand.Execute(null);
            e.Handled = true;
            LogTunnelEvent("HotkeyService", e, vm, "palette-esc-closed");
            return true;
        }

        var handled = TryHandleTunnelShortcuts(e, vm);
        if (!handled && (e.KeyModifiers != KeyModifiers.None || e.Key is Key.Escape or Key.F5 or Key.F10 or Key.F11))
            LogTunnelEvent("HotkeyService", e, vm, "no-match");
        return handled;
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
    /// Единственный путь выполнения глобальных хоткеев окна (TOML + <see cref="KeyGestureChordMatching"/>).
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

            if (!KeyGestureChordMatching.Matches(g, e))
                continue;

            if (tomlKey.Equals("toggle_command_palette", StringComparison.OrdinalIgnoreCase))
            {
                if (!vm.ToggleCommandPaletteCommand.CanExecute(null))
                {
                    LogTunnelEvent("HotkeyService", e, vm, $"matched-{tomlKey}-cannot-execute");
                    return false;
                }
                vm.ToggleCommandPaletteCommand.Execute(null);
                e.Handled = true;
                LogTunnelEvent("HotkeyService", e, vm, $"matched-{tomlKey}");
                return true;
            }

            var (cmd, param) = ResolveWindowCommand(vm, wh);
            if (!cmd.CanExecute(param))
            {
                LogTunnelEvent("HotkeyService", e, vm, $"matched-{tomlKey}-cannot-execute");
                return false;
            }
            cmd.Execute(param);
            e.Handled = true;
            LogTunnelEvent("HotkeyService", e, vm, $"matched-{tomlKey}");
            return true;
        }

        return false;
    }
}
