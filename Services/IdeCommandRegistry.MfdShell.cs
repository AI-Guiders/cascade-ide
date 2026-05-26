using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: страницы оболочки Mfd (без TabControl; v1 — колонка зоны Mfd).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterMfdShellPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        AddPalette(b, "mfd_shell_page_workspace", IdeCommands.SetMfdShellPage, "Вторичный контур: WORKSPACE (здоровье)", "Вторичный контур", """{"page":"WorkspaceHealth"}""");
        AddPalette(b, "mfd_shell_page_related_files", IdeCommands.SetMfdShellPage, "Вторичный контур: Связанные файлы", "Вторичный контур", """{"page":"RelatedFiles"}""");
        AddPalette(b, "mfd_shell_page_chat", IdeCommands.SetMfdShellPage, "Вторичный контур: Чат", "Вторичный контур", """{"page":"Chat"}""");
        AddPalette(b, "mfd_shell_page_ai_settings", IdeCommands.SetMfdShellPage, "Вторичный контур: Параметры", "Вторичный контур", """{"page":"AiChatSettings"}""");
        AddPalette(b, "mfd_shell_page_terminal", IdeCommands.SetMfdShellPage, "Вторичный контур: Терминал", "Вторичный контур", """{"page":"Terminal"}""");
        AddPalette(b, "mfd_shell_page_build", IdeCommands.SetMfdShellPage, "Вторичный контур: Сборка · вывод", "Вторичный контур", """{"page":"Build"}""");
        AddPalette(b, "mfd_shell_page_problems", IdeCommands.SetMfdShellPage, "Вторичный контур: Problems", "Вторичный контур", """{"page":"Problems"}""");
        AddPalette(b, "mfd_shell_page_git", IdeCommands.SetMfdShellPage, "Вторичный контур: Git", "Вторичный контур", """{"page":"Git"}""");
        AddPalette(b, "mfd_shell_page_events", IdeCommands.SetMfdShellPage, "Вторичный контур: События", "Вторичный контур", """{"page":"Events"}""");
        AddPalette(b, "mfd_shell_page_tests", IdeCommands.SetMfdShellPage, "Вторичный контур: Тесты", "Вторичный контур", """{"page":"Tests"}""");
        AddPalette(b, "mfd_shell_page_hypotheses", IdeCommands.SetMfdShellPage, "Вторичный контур: Гипотезы", "Вторичный контур", """{"page":"Hypotheses"}""");
        AddPalette(b, "mfd_shell_page_debug_stack", IdeCommands.SetMfdShellPage, "Вторичный контур: Отладка · стек", "Вторичный контур", """{"page":"DebugStack"}""");
    }
}
