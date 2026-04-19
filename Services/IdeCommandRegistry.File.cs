using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: меню «Файл» (см. <see cref="IdeCommands"/> в <c>IdeCommands.Chrome.cs</c>).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterFileMenu(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        // ——— Меню «Файл» / приложение
        AddPalette(b, "open_solution_dialog", IdeCommands.OpenSolutionDialog, "Открыть решение…", "Файл");
        AddPalette(b, "open_folder_dialog", IdeCommands.OpenFolderDialog, "Открыть папку…", "Файл");
        AddPalette(b, "open_file_dialog", IdeCommands.OpenFileDialog, "Открыть файл…", "Файл");
        AddPalette(b, "export_expanded_markdown", IdeCommands.ExportExpandedMarkdown, "Export expanded Markdown…", "Файл");
        AddPalette(b, "exit_application", IdeCommands.ExitApplication, "Выход", "Файл");
    }
}
