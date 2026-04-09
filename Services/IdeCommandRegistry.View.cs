using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: «Вид», тема, превью, палитра команд (см. <c>IdeCommands.Chrome.cs</c>, <c>IdeCommands.UiVisibility.cs</c>).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterView(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        // ——— Вид: фокус и панели (переключатели)
        AddPalette(b, "focus_editor", IdeCommands.FocusEditor, "Фокус в редактор", "Вид");
        AddPalette(b, "toggle_solution_explorer", IdeCommands.ToggleSolutionExplorer, "Переключить обозреватель решения", "Вид");
        AddPalette(b, "toggle_build_output", IdeCommands.ToggleBuildOutput, "Переключить вывод сборки", "Вид");
        AddPalette(b, "toggle_terminal", IdeCommands.ToggleTerminal, "Переключить терминал", "Вид");
        AddPalette(b, "toggle_chat_panel", IdeCommands.ToggleChatPanel, "Переключить чат", "Вид");
        AddPalette(b, "toggle_git_panel", IdeCommands.ToggleGitPanel, "Переключить панель Git", "Вид");
        AddPalette(b, "toggle_instrumentation_dock", IdeCommands.ToggleInstrumentationDock, "Переключить док инструментирования", "Вид");

        // ——— Вид: тулбар — явные панели
        AddPalette(b, "show_solution_explorer_panel", IdeCommands.ShowSolutionExplorerPanel, "Показать обозреватель решения", "Вид");
        AddPalette(b, "show_build_output_panel", IdeCommands.ShowBuildOutputPanel, "Показать вывод сборки", "Вид");
        AddPalette(b, "show_chat_panel", IdeCommands.ShowChatPanel, "Показать чат", "Вид");
        AddPalette(b, "show_terminal_panel", IdeCommands.ShowTerminalPanel, "Показать терминал", "Вид");
        AddPalette(b, "hide_build_output_panel", IdeCommands.HideBuildOutputPanel, "Скрыть вывод сборки", "Вид");

        // ——— Вид: группы редакторов
        AddPalette(b, "set_single_editor_group", IdeCommands.SetSingleEditorGroup, "Одна группа редакторов", "Вид");
        AddPalette(b, "set_dual_editor_group", IdeCommands.SetDualEditorGroup, "Две группы редакторов", "Вид");
        AddPalette(b, "set_triple_editor_group", IdeCommands.SetTripleEditorGroup, "Три группы редакторов", "Вид");

        // ——— Вид: режим (Focus / Balanced / Power, cycle, set_ui_mode)
        AddPalette(b, "set_focus_mode", IdeCommands.SetFocusModeUi, "Режим: Focus", "Вид");
        AddPalette(b, "set_balanced_mode", IdeCommands.SetBalancedModeUi, "Режим: Balanced", "Вид");
        AddPalette(b, "set_power_mode", IdeCommands.SetPowerModeUi, "Режим: Power", "Вид");
        AddPalette(
            b,
            "cycle_ui_mode",
            IdeCommands.CycleUiMode,
            "Следующий UI-режим",
            "Вид",
            allowed: null,
            access: CommandAccessibleFrom.AgentAndUI,
            window: new MainWindowHotkeyVmBinding(MainWindowHotkeyVmBindingKind.CycleUiMode));

        AddPalette(b, "set_ui_mode_Focus", IdeCommands.SetUiMode, "Режим интерфейса: Focus", "Вид", """{"mode":"Focus"}""");
        AddPalette(b, "set_ui_mode_Editor", IdeCommands.SetUiMode, "Режим интерфейса: Editor", "Вид", """{"mode":"Editor"}""");
        AddPalette(b, "set_ui_mode_Balanced", IdeCommands.SetUiMode, "Режим интерфейса: Balanced", "Вид", """{"mode":"Balanced"}""");
        AddPalette(b, "set_ui_mode_Power", IdeCommands.SetUiMode, "Режим интерфейса: Power", "Вид", """{"mode":"Power"}""");
        AddPalette(b, "set_ui_mode_AgentChat", IdeCommands.SetUiMode, "Режим интерфейса: Agent Chat", "Вид", """{"mode":"AgentChat"}""");
        AddPalette(b, "set_ui_mode_Debug", IdeCommands.SetUiMode, "Режим интерфейса: Debug", "Вид", """{"mode":"Debug"}""");
        AddPalette(b, "set_ui_mode_Flight", IdeCommands.SetUiMode, "Режим интерфейса: Flight", "Вид", """{"mode":"Flight"}""");

        // ——— Вид: тема
        AddPalette(b, "apply_light_theme", IdeCommands.ApplyLightTheme, "Тема: светлая", "Вид");
        AddPalette(b, "apply_dark_theme", IdeCommands.ApplyDarkTheme, "Тема: тёмная", "Вид");
        AddPalette(b, "apply_cursor_like_theme", IdeCommands.ApplyCursorLikeTheme, "Тема: как Cursor", "Вид");
        AddPalette(b, "apply_power_classic_theme", IdeCommands.ApplyPowerClassicTheme, "Тема: Power классическая", "Вид");
        AddPalette(b, "open_theme_file_dialog", IdeCommands.OpenThemeFileDialog, "Открыть файл темы…", "Вид");

        // ——— Вид: превью и вспомогательное окно
        AddPalette(b, "open_preview_window", IdeCommands.OpenPreviewWindow, "Превью в отдельном окне", "Вид");
        AddPalette(b, "toggle_auxiliary_workspace_window", IdeCommands.ToggleAuxiliaryWorkspaceWindow, "Второе окно рабочей области…", "Вид");

        // ——— Вид: палитра команд (глобальный хоткей окна)
        AddPalette(
            b,
            "toggle_command_palette",
            IdeCommands.ToggleCommandPalette,
            "Палитра команд",
            "Вид",
            window: new MainWindowHotkeyVmBinding(MainWindowHotkeyVmBindingKind.ToggleCommandPalette));
    }
}
