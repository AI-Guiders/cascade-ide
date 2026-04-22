using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: «Вид», тема, превью, палитра команд (см. <c>IdeCommands.Chrome.cs</c>, <c>IdeCommands.UiVisibility.cs</c>).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterView(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        // ——— Вид: фокус и панели (переключатели)
        AddPalette(b, "focus_editor", IdeCommands.FocusEditor, "Фокус в редактор", "Вид");
        AddPalette(b, "toggle_pfd_region_expanded", IdeCommands.TogglePfdRegionExpanded, "Переключить регион Pfd (Semantic Map)", "Вид");
        AddPalette(b, "cycle_code_navigation_map_presentation", IdeCommands.CycleCodeNavigationMapPresentation, "Semantic Map: цикл вида (list / graph / both)", "Вид");
        AddPalette(b, "cycle_code_navigation_map_level", IdeCommands.CycleCodeNavigationMapLevel, "Semantic Map: уровень file ↔ control flow", "Вид");
        AddPalette(b, "cycle_code_navigation_map_detail_level", IdeCommands.CycleCodeNavigationMapDetailLevel, "Semantic Map: детализация glance / normal / inspect", "Вид");
        AddPalette(b, "toggle_build_output", IdeCommands.ToggleBuildOutput, "Переключить вывод сборки", "Вид");
        AddPalette(b, "toggle_terminal", IdeCommands.ToggleTerminal, "Переключить терминал", "Вид");
        AddPalette(b, "toggle_workspace_splitters_lock", IdeCommands.ToggleWorkspaceSplittersLock, "TOL: ON GND / IN AIR (сплиттеры рабочей области)", "Вид");
        AddPalette(b, "toggle_mfd_region_expanded", IdeCommands.ToggleMfdRegionExpanded, "Переключить регион Mfd", "Вид");
        AddPalette(b, "toggle_git_panel", IdeCommands.ToggleGitPanel, "Переключить панель Git", "Вид");
        AddPalette(b, "toggle_instrumentation_dock", IdeCommands.ToggleInstrumentationDock, "Переключить док инструментирования", "Вид");

        // ——— Вид: тулбар — явные панели
        AddPalette(b, "show_pfd_region_panel", IdeCommands.ShowPfdRegionPanel, "Развернуть регион Pfd", "Вид");
        AddPalette(b, "show_build_output_panel", IdeCommands.ShowBuildOutputPanel, "Показать вывод сборки", "Вид");
        AddPalette(b, "show_chat_page", IdeCommands.ShowChatPage, "Страница Chat (регион Mfd)", "Вид");
        AddPalette(b, "show_solution_explorer_page", IdeCommands.ShowSolutionExplorerPage, "Страница обозревателя решения (регион Mfd)", "Вид");
        AddPalette(b, "show_related_files_mfd_page", IdeCommands.ShowRelatedFilesMfdPage, "Страница «Связанные файлы» (регион Mfd)", "Вид");
        AddPalette(b, "show_terminal_panel", IdeCommands.ShowTerminalPanel, "Показать терминал", "Вид");
        AddPalette(b, "hide_build_output_panel", IdeCommands.HideBuildOutputPanel, "Скрыть вывод сборки", "Вид");

        // ——— Вид: группы редакторов
        AddPalette(b, "set_single_editor_group", IdeCommands.SetSingleEditorGroup, "Одна группа редакторов", "Вид");
        AddPalette(b, "set_dual_editor_group", IdeCommands.SetDualEditorGroup, "Две группы редакторов", "Вид");
        AddPalette(b, "set_triple_editor_group", IdeCommands.SetTripleEditorGroup, "Три группы редакторов", "Вид");

        // ——— Вид: тема
        AddPalette(b, "apply_light_theme", IdeCommands.ApplyLightTheme, "Тема: светлая", "Вид");
        AddPalette(b, "apply_dark_theme", IdeCommands.ApplyDarkTheme, "Тема: тёмная", "Вид");
        AddPalette(b, "apply_cursor_like_theme", IdeCommands.ApplyCursorLikeTheme, "Тема: как Cursor", "Вид");
        AddPalette(b, "apply_power_classic_theme", IdeCommands.ApplyPowerClassicTheme, "Тема: Power классическая", "Вид");
        AddPalette(b, "open_theme_file_dialog", IdeCommands.OpenThemeFileDialog, "Открыть файл темы…", "Вид");

        // ——— Вид: превью Markdown
        AddPalette(b, "show_markdown_preview_page", IdeCommands.ShowMarkdownPreviewPage, "Показать Markdown preview (MFD)", "Вид");
        AddPalette(b, "open_preview_window", IdeCommands.OpenPreviewWindow, "Превью в отдельном окне", "Вид");

        // ——— Вид: палитра команд (глобальный хоткей окна)
        AddPalette(
            b,
            "toggle_command_palette",
            IdeCommands.ToggleCommandPalette,
            "Палитра команд",
            "Вид",
            window: new MainWindowHotkeyVmBinding(MainWindowHotkeyVmBindingKind.ToggleCommandPalette));

        AddPalette(b, "show_environment_readiness_page", IdeCommands.ShowEnvironmentReadinessPage, "Готовность окружения (LSP, dotnet…)", "Вид");
        AddPalette(b, "close_environment_readiness_page", IdeCommands.CloseEnvironmentReadinessPage, "Закрыть страницу готовности окружения", "Вид");
    }
}
