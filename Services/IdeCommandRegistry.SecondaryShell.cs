using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: страницы вторичного контура оболочки (без TabControl; v1 — колонка зоны Mfd).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterSecondaryShellPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        AddPalette(b, "secondary_shell_page_workspace", IdeCommands.SetSecondaryShellPage, "Вторичный контур: WORKSPACE (здоровье)", "Вторичный контур", """{"page":"WorkspaceHealth"}""");
        AddPalette(b, "secondary_shell_page_chat", IdeCommands.SetSecondaryShellPage, "Вторичный контур: Чат", "Вторичный контур", """{"page":"Chat"}""");
        AddPalette(b, "secondary_shell_page_ai_settings", IdeCommands.SetSecondaryShellPage, "Вторичный контур: Параметры AI и чата", "Вторичный контур", """{"page":"AiChatSettings"}""");
        AddPalette(b, "secondary_shell_page_terminal", IdeCommands.SetSecondaryShellPage, "Вторичный контур: Терминал", "Вторичный контур", """{"page":"Terminal"}""");
        AddPalette(b, "secondary_shell_page_build", IdeCommands.SetSecondaryShellPage, "Вторичный контур: Сборка · вывод", "Вторичный контур", """{"page":"Build"}""");
        AddPalette(b, "secondary_shell_page_problems", IdeCommands.SetSecondaryShellPage, "Вторичный контур: Problems", "Вторичный контур", """{"page":"Problems"}""");
        AddPalette(b, "secondary_shell_page_git", IdeCommands.SetSecondaryShellPage, "Вторичный контур: Git", "Вторичный контур", """{"page":"Git"}""");
        AddPalette(b, "secondary_shell_page_events", IdeCommands.SetSecondaryShellPage, "Вторичный контур: События", "Вторичный контур", """{"page":"Events"}""");
        AddPalette(b, "secondary_shell_page_tests", IdeCommands.SetSecondaryShellPage, "Вторичный контур: Тесты", "Вторичный контур", """{"page":"Tests"}""");
        AddPalette(b, "secondary_shell_page_hypotheses", IdeCommands.SetSecondaryShellPage, "Вторичный контур: Гипотезы", "Вторичный контур", """{"page":"Hypotheses"}""");
        AddPalette(b, "secondary_shell_page_debug_stack", IdeCommands.SetSecondaryShellPage, "Вторичный контур: Отладка · стек", "Вторичный контур", """{"page":"DebugStack"}""");
    }
}
