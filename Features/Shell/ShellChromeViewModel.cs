using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using CascadeIDE.Models.Shell;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Shell;

/// <summary>
/// Состояние оболочки главного окна: регионы MainGrid, страница MFD, режим UI, сборка на полосе.
/// <see cref="MainWindowViewModel"/> — композитор (прокси + presentation/reactive side-effects).
/// </summary>
public sealed partial class ShellChromeViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _host;

    public ShellChromeViewModel(MainWindowViewModel host) => _host = host;

    /// <summary>Bootstrap из настроек до первой UI-привязки (без side-effects host).</summary>
    internal void ApplyBootstrapFromSettings(CascadeIdeSettings settings)
    {
#pragma warning disable MVVMTK0034
        _isPfdRegionExpanded = settings.Workspace.PfdExpanded;
        _isTerminalVisible = settings.Workspace.ShowTerminal;
        _isGitPanelVisible = settings.Workspace.ShowGit;
        _isInstrumentationDockVisible = settings.Workspace.ShowInstrumentation;
        _uiMode = UiChromeViewModel.NormalizeUiMode(settings.Workspace.Mode);
#pragma warning restore MVVMTK0034
    }

    /// <summary>
    /// Intent геометрии: регион Mfd в <c>MainGrid</c> развёрнут (ширина по режиму) или свёрнут.
    /// Страница «Чат» — <see cref="MfdShellPage.Chat"/> через <see cref="CurrentMfdShellPage"/>, отдельно.
    /// </summary>
    [ObservableProperty]
    private bool _isMfdRegionExpanded = true;

    /// <summary>
    /// Intent геометрии: регион Pfd в <c>MainGrid</c> развёрнут (ширина по workspace/режиму) или свёрнут.
    /// </summary>
    [ObservableProperty]
    private bool _isPfdRegionExpanded = true;

    /// <summary>Страница «Терминал» в колонке MFD (меню Вид → Терминал).</summary>
    [ObservableProperty]
    private bool _isTerminalVisible;

    /// <summary>Страница «Git» в колонке MFD (меню Вид → Git).</summary>
    [ObservableProperty]
    private bool _isGitPanelVisible;

    /// <summary>Страница «Вывод сборки» в колонке MFD.</summary>
    [ObservableProperty]
    private bool _isBuildOutputVisible;

    /// <summary>Док инструментирования в колонке MFD.</summary>
    [ObservableProperty]
    private bool _isInstrumentationDockVisible = true;

    /// <summary>Какая страница показана в оболочке Mfd.</summary>
    [ObservableProperty]
    private MfdShellPage _currentMfdShellPage = MfdShellPage.Terminal;

    [ObservableProperty]
    private CommandPaletteHost _commandPaletteHost = CommandPaletteHost.MainWindow;

    [ObservableProperty]
    private string _uiMode = "Balanced";

    [ObservableProperty]
    private int _editorGroupCount = 1;

    /// <summary>Снимок раскладки UI (JSON), полоса Workspace Health в Power.</summary>
    [ObservableProperty]
    private string _workspaceSnapshotJson = "";

    [ObservableProperty]
    private bool _isBuilding;
}
