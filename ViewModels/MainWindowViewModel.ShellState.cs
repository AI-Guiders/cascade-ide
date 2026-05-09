namespace CascadeIDE.ViewModels;

/// <summary>
/// Состояние раскладки главного окна: три зоны внимания в <c>MainGrid</c> (PFD · Forward · MFD), см. ADR 0021 и <c>docs/ux/cascade-ide-ui-layout-v1.md</c>.
/// Терминал, сборка, Git и пр. — во вторичном контуре колонки MFD (<c>MfdShellView</c> / <c>MfdShellPageStack</c>); отдельной строки «нижней панели» на всю ширину под сеткой нет.
/// Режим ИИ и облачные ключи — <c>MainWindowViewModel.ShellState.AiProviders.cs</c>; чат и MCP/ACP — <c>MainWindowViewModel.ShellState.ChatAndSessionConfig.cs</c>; полоса агента / тесты для IDE Health — <c>MainWindowViewModel.ShellState.AutonomousAgentStripe.cs</c>;
/// регион MFD/PFD и страницы контура — <c>ShellState.RegionAndContour.cs</c>; режим/UI-сессия и полосы — <c>ShellState.UiSessionChrome.cs</c>; модель/Kroki — <c>ShellState.ModelPullMarkdown.cs</c>.
/// </summary>
public partial class MainWindowViewModel
{
    public string EditorTextGroup2 => Documents.SelectedDocumentGroup2?.Content ?? "";

    public string EditorTextGroup3 => Documents.SelectedDocumentGroup3?.Content ?? "";
}
