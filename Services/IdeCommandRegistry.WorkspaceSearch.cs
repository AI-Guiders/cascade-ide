using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: поиск по файлам workspace (ripgrep).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterWorkspaceSearchPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        AddPalette(
            b,
            "search_workspace_text",
            IdeCommands.SearchWorkspaceText,
            "Поиск текста в workspace (ripgrep)…",
            "Рабочая область",
            args: """{"pattern":"","glob":"*.cs","max_matches":200}""");
    }
}
