using System.Collections.Immutable;
using System.Text.Json;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Services;

/// <summary>
/// Статический список команд для палитры (ADR 0013): id команды MCP + заголовок + категория + опционально JSON args.
/// </summary>
public static class IdeCommandPaletteCatalog
{
    /// <param name="AllowedFamilies">Если задано и не пусто — команда доступна только в перечисленных семействах UI-режима.</param>
    public sealed record Entry(
        string PaletteId,
        string CommandId,
        string Title,
        string Category,
        string? ArgsJson = null,
        ImmutableArray<UiModeFamily>? AllowedFamilies = null);

    public static ImmutableArray<Entry> All { get; } = Build();

    private static ImmutableArray<Entry> Build()
    {
        var b = ImmutableArray.CreateBuilder<Entry>();

        void add(string paletteId, string commandId, string title, string category, string? args = null, params UiModeFamily[] allowed)
        {
            ImmutableArray<UiModeFamily>? fam = allowed.Length > 0 ? ImmutableArray.Create(allowed) : null;
            b.Add(new Entry(paletteId, commandId, title, category, args, fam));
        }

        // Файл
        add("open_solution_dialog", IdeCommands.OpenSolutionDialog, "Открыть решение…", "Файл");
        add("open_file_dialog", IdeCommands.OpenFileDialog, "Открыть файл…", "Файл");
        add("export_expanded_markdown", IdeCommands.ExportExpandedMarkdown, "Export expanded Markdown…", "Файл");
        add("exit_application", IdeCommands.ExitApplication, "Выход", "Файл");

        // Вид — фокус и панели
        add("focus_editor", IdeCommands.FocusEditor, "Фокус в редактор", "Вид");
        add("toggle_solution_explorer", IdeCommands.ToggleSolutionExplorer, "Переключить обозреватель решения", "Вид");
        add("toggle_build_output", IdeCommands.ToggleBuildOutput, "Переключить вывод сборки", "Вид");
        add("toggle_terminal", IdeCommands.ToggleTerminal, "Переключить терминал", "Вид");
        add("toggle_chat_panel", IdeCommands.ToggleChatPanel, "Переключить чат", "Вид");
        add("toggle_git_panel", IdeCommands.ToggleGitPanel, "Переключить панель Git", "Вид");
        add("toggle_instrumentation_dock", IdeCommands.ToggleInstrumentationDock, "Переключить док инструментирования", "Вид");

        add("show_solution_explorer_panel", IdeCommands.ShowSolutionExplorerPanel, "Показать обозреватель решения", "Вид");
        add("show_build_output_panel", IdeCommands.ShowBuildOutputPanel, "Показать вывод сборки", "Вид");
        add("show_chat_panel", IdeCommands.ShowChatPanel, "Показать чат", "Вид");
        add("show_terminal_panel", IdeCommands.ShowTerminalPanel, "Показать терминал", "Вид");
        add("hide_build_output_panel", IdeCommands.HideBuildOutputPanel, "Скрыть вывод сборки", "Вид");

        add("set_single_editor_group", IdeCommands.SetSingleEditorGroup, "Одна группа редакторов", "Вид");
        add("set_dual_editor_group", IdeCommands.SetDualEditorGroup, "Две группы редакторов", "Вид");
        add("set_triple_editor_group", IdeCommands.SetTripleEditorGroup, "Три группы редакторов", "Вид");

        add("set_focus_mode", IdeCommands.SetFocusModeUi, "Режим: Focus", "Вид");
        add("set_balanced_mode", IdeCommands.SetBalancedModeUi, "Режим: Balanced", "Вид");
        add("set_power_mode", IdeCommands.SetPowerModeUi, "Режим: Power", "Вид");
        add("cycle_ui_mode", IdeCommands.CycleUiMode, "Следующий UI-режим", "Вид");

        add("set_ui_mode_Focus", IdeCommands.SetUiMode, "Режим интерфейса: Focus", "Вид", """{"mode":"Focus"}""");
        add("set_ui_mode_Editor", IdeCommands.SetUiMode, "Режим интерфейса: Editor", "Вид", """{"mode":"Editor"}""");
        add("set_ui_mode_Balanced", IdeCommands.SetUiMode, "Режим интерфейса: Balanced", "Вид", """{"mode":"Balanced"}""");
        add("set_ui_mode_Power", IdeCommands.SetUiMode, "Режим интерфейса: Power", "Вид", """{"mode":"Power"}""");
        add("set_ui_mode_AgentChat", IdeCommands.SetUiMode, "Режим интерфейса: Agent Chat", "Вид", """{"mode":"AgentChat"}""");
        add("set_ui_mode_Debug", IdeCommands.SetUiMode, "Режим интерфейса: Debug", "Вид", """{"mode":"Debug"}""");
        add("set_ui_mode_Flight", IdeCommands.SetUiMode, "Режим интерфейса: Flight", "Вид", """{"mode":"Flight"}""");

        add("apply_light_theme", IdeCommands.ApplyLightTheme, "Тема: светлая", "Вид");
        add("apply_dark_theme", IdeCommands.ApplyDarkTheme, "Тема: тёмная", "Вид");
        add("apply_cursor_like_theme", IdeCommands.ApplyCursorLikeTheme, "Тема: как Cursor", "Вид");
        add("apply_power_classic_theme", IdeCommands.ApplyPowerClassicTheme, "Тема: Power классическая", "Вид");
        add("open_theme_file_dialog", IdeCommands.OpenThemeFileDialog, "Открыть файл темы…", "Вид");
        add("open_preview_window", IdeCommands.OpenPreviewWindow, "Превью в отдельном окне", "Вид");
        add("toggle_auxiliary_workspace_window", IdeCommands.ToggleAuxiliaryWorkspaceWindow, "Второе окно рабочей области…", "Вид");

        // Сборка
        add("build_solution_ui", IdeCommands.BuildSolutionUi, "Собрать решение (UI)", "Сборка");
        add("build", IdeCommands.Build, "Сборка (structured)", "Сборка");

        // Отладка — в палитре только при семействе Debug (UX: недоступные строки серые).
        add("debug_continue", IdeCommands.DebugContinue, "Отладка: продолжить", "Отладка", null, UiModeFamily.Debug);
        add("debug_step_over", IdeCommands.DebugStepOver, "Отладка: шаг с обходом", "Отладка", null, UiModeFamily.Debug);
        add("debug_step_into", IdeCommands.DebugStepInto, "Отладка: шаг с заходом", "Отладка", null, UiModeFamily.Debug);
        add("debug_step_out", IdeCommands.DebugStepOut, "Отладка: шаг с выходом", "Отладка", null, UiModeFamily.Debug);
        add("debug_stop", IdeCommands.DebugStop, "Отладка: остановить", "Отладка", null, UiModeFamily.Debug);
        add("debug_ping", IdeCommands.DebugPing, "Отладка: ping", "Отладка", null, UiModeFamily.Debug);

        // Git
        add("git_status", IdeCommands.GitStatus, "Git: статус", "Git");

        // Документы
        add("reopen_closed_document", IdeCommands.ReopenClosedDocument, "Открыть закрытую вкладку", "Документы");

        // Настройки и справка
        add("open_settings", IdeCommands.OpenSettings, "Параметры…", "Настройки");
        add("about", IdeCommands.About, "О программе", "Справка");

        return b.ToImmutable();
    }

    /// <summary>Парсит JSON объекта args для <see cref="IdeMcpCommandExecutor.ExecuteAsync"/>.</summary>
    public static IReadOnlyDictionary<string, JsonElement>? ParseArgs(string? argsJson)
    {
        if (string.IsNullOrWhiteSpace(argsJson))
            return null;
        using var doc = JsonDocument.Parse(argsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return null;
        var d = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var p in doc.RootElement.EnumerateObject())
            d[p.Name] = p.Value.Clone();
        return d;
    }
}
