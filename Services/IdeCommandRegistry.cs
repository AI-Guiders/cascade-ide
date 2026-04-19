using System.Collections.Immutable;
using System.Text.Json;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Services;

/// <summary>
/// Единый реестр: <see cref="CommandId"/> для MCP (если есть), метаданные палитры, доступность для агента/UI,
/// привязки глобальных хоткеев главного окна. Жесты — только в <c>hotkeys.toml</c> (ключ = <see cref="EffectiveHotkeysTomlKey"/>).
/// См. <c>docs/design/ide-command-registry-v1.md</c>, <c>docs/adr/0030-command-ids-hotkeys-and-ui-registry-layers.md</c>.
/// Части каталога — partial-файлы <c>IdeCommandRegistry.*.cs</c> (как <c>IdeCommands.*.cs</c>).
/// </summary>
public static partial class IdeCommandRegistry
{
    /// <summary>Нет отдельного <c>IdeCommands</c>-id: только UI «Начать или продолжить» (F5); агент — <c>debug_launch</c> / <c>debug_continue</c> и т.д.</summary>
    public const string DebugStartOrContinueHotkeyId = "debug_start_or_continue";

    /// <summary>Последний индекс для <c>set_ui_mode_by_index_*</c> и tunnel-порядка (включительно).</summary>
    private const int SetUiModeByIndexMaxInclusive = 8;

    public enum CommandAccessibleFrom
    {
        /// <summary>MCP <c>ide_execute_command</c> + палитра/хоткеи (где подключено).</summary>
        AgentAndUI,

        /// <summary>Только UI (жесты, меню); для того же эффекта у агента — другие <c>command_id</c> (ADR 0030).</summary>
        UIOnly,
    }

    public enum MainWindowHotkeyVmBindingKind
    {
        ToggleCommandPalette,
        CycleUiMode,
        SetUiModeByIndex,
        DebugStartOrContinue,
        DebugStop,
        DebugStepOver,
        DebugStepInto,
        DebugStepOut,
    }

    /// <summary>Привязка хоткея окна к команде VM (без строк жестов — они в TOML).</summary>
    public readonly record struct MainWindowHotkeyVmBinding(
        MainWindowHotkeyVmBindingKind Kind,
        int SetUiModeIndex = 0);

    public sealed record IdeCommandRegistryEntry(
        string PaletteId,
        /// <summary>Id для <c>ide_execute_command</c>; <see langword="null"/> — строка не для MCP (только UI).</summary>
        string? CommandId,
        string Title,
        string Category,
        string? ArgsJson,
        ImmutableArray<UiModeFamily>? AllowedFamilies,
        CommandAccessibleFrom AccessibleFrom,
        /// <summary>Показывать в палитре команд.</summary>
        bool IncludeInPalette,
        /// <summary>Глобальный жест на главном окне + tunnel при фокусе в редакторе.</summary>
        MainWindowHotkeyVmBinding? WindowHotkey,
        /// <summary>Ключ в <c>hotkeys.toml</c>, если не совпадает с <see cref="CommandId"/>.</summary>
        string? HotkeysTomlKey = null)
    {
        /// <summary>Ключ для мерджа TOML и подсказок.</summary>
        public string EffectiveHotkeysTomlKey => HotkeysTomlKey ?? CommandId ?? PaletteId;
    }

    public static ImmutableArray<IdeCommandRegistryEntry> AllEntries { get; } = Build();

    /// <summary>Индекс <see cref="EffectiveHotkeysTomlKey"/> → запись с <see cref="IdeCommandRegistryEntry.WindowHotkey"/>.</summary>
    public static IReadOnlyDictionary<string, IdeCommandRegistryEntry> WindowHotkeyByTomlKey { get; } = BuildWindowHotkeyIndex();

    /// <summary>
    /// Детерминированный порядок tunnel (первый совпавший жест выигрывает; важно для пересечений).
    /// Единственный источник списка ключей; множество ключей должно совпадать с множеством <see cref="WindowHotkeyByTomlKey"/> (см. тесты).
    /// </summary>
    public static ImmutableArray<string> TunnelShortcutKeyOrder { get; } = BuildTunnelKeyPriorityOrder();

    private static ImmutableArray<IdeCommandRegistryEntry> Build()
    {
        var b = ImmutableArray.CreateBuilder<IdeCommandRegistryEntry>();
        RegisterFileMenu(b);
        RegisterView(b);
        RegisterWorkspaceSearchPalette(b);
        RegisterBuildPalette(b);
        RegisterDebugPalette(b);
        RegisterGitPalette(b);
        RegisterDocumentsPalette(b);
        RegisterSettingsAndHelpPalette(b);
        RegisterSecondaryShellPalette(b);
        RegisterWindowOnlyHotkeys(b);
        return b.ToImmutable();
    }

    private static void AddPalette(
        ImmutableArray<IdeCommandRegistryEntry>.Builder b,
        string paletteId,
        string commandId,
        string title,
        string category,
        string? args = null,
        ImmutableArray<UiModeFamily>? allowed = null,
        CommandAccessibleFrom access = CommandAccessibleFrom.AgentAndUI,
        MainWindowHotkeyVmBinding? window = null,
        string? hotkeysTomlKey = null)
    {
        b.Add(new IdeCommandRegistryEntry(
            paletteId,
            commandId,
            title,
            category,
            args,
            allowed,
            access,
            IncludeInPalette: true,
            window,
            hotkeysTomlKey));
    }

    private static void AddWindowOnly(
        ImmutableArray<IdeCommandRegistryEntry>.Builder b,
        string paletteId,
        string? commandId,
        MainWindowHotkeyVmBinding window,
        CommandAccessibleFrom access,
        string? hotkeysTomlKey = null)
    {
        b.Add(new IdeCommandRegistryEntry(
            paletteId,
            commandId,
            Title: "",
            Category: "",
            ArgsJson: null,
            AllowedFamilies: null,
            access,
            IncludeInPalette: false,
            window,
            hotkeysTomlKey));
    }

    /// <summary>Единственное место, где задаётся порядок и состав ключей для tunnel (в т.ч. <c>set_ui_mode_by_index_*</c>).</summary>
    private static ImmutableArray<string> BuildTunnelKeyPriorityOrder()
    {
        var builder = ImmutableArray.CreateBuilder<string>();
        builder.Add("toggle_command_palette");
        builder.Add(DebugStartOrContinueHotkeyId);
        builder.Add(IdeCommands.DebugStop);
        builder.Add(IdeCommands.DebugStepOver);
        builder.Add(IdeCommands.DebugStepInto);
        builder.Add(IdeCommands.DebugStepOut);
        builder.Add("cycle_ui_mode");
        for (var i = 0; i <= SetUiModeByIndexMaxInclusive; i++)
            builder.Add($"set_ui_mode_by_index_{i}");
        return builder.ToImmutable();
    }

    private static Dictionary<string, IdeCommandRegistryEntry> BuildWindowHotkeyIndex()
    {
        var d = new Dictionary<string, IdeCommandRegistryEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in AllEntries)
        {
            if (e.WindowHotkey is null)
                continue;
            var key = e.EffectiveHotkeysTomlKey;
            if (!d.TryAdd(key, e))
                throw new InvalidOperationException(
                    $"Дубликат EffectiveHotkeysTomlKey для окна: {key} (палитра/порядок: {e.PaletteId}).");
        }

        return d;
    }

    /// <summary>Парсит JSON args для <see cref="IdeMcpCommandExecutor.ExecuteAsync"/> (как в прежнем каталоге палитры).</summary>
    public static IReadOnlyDictionary<string, JsonElement>? ParseArgs(string? argsJson)
    {
        if (string.IsNullOrWhiteSpace(argsJson))
            return null;
        using var doc = JsonDocument.Parse(argsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return null;
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var p in doc.RootElement.EnumerateObject())
            result[p.Name] = p.Value.Clone();
        return result;
    }
}
