using CascadeIDE.Features.Build;
using CascadeIDE.Features.Debug;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Shell;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Wave 2: прокси RelayCommand на feature VMs (XAML, MCP, hotkeys без смены привязок).
/// </summary>
public partial class MainWindowViewModel
{
    public IRelayCommand OpenSolutionCommand => ApplicationShell.OpenSolutionCommand;

    public IRelayCommand CreateNewSolutionCommand => ApplicationShell.CreateNewSolutionCommand;

    public IRelayCommand OpenFolderCommand => ApplicationShell.OpenFolderCommand;

    public IRelayCommand OpenFileFromDialogCommand => ApplicationShell.OpenFileFromDialogCommand;

    public IRelayCommand ExitCommand => ApplicationShell.ExitCommand;

    public IRelayCommand AboutCommand => ApplicationShell.AboutCommand;

    public IRelayCommand OpenSettingsCommand => ApplicationShell.OpenSettingsCommand;

    public IRelayCommand OpenFullSettingsWindowCommand => ApplicationShell.OpenFullSettingsWindowCommand;

    public IRelayCommand ToggleMfdHostWindowCommand => ApplicationShell.ToggleMfdHostWindowCommand;

    public IRelayCommand TogglePmSplitHostWindowCommand => ApplicationShell.TogglePmSplitHostWindowCommand;

    public IAsyncRelayCommand ApplyDarkThemeCommand => ApplicationShell.ApplyDarkThemeCommand;

    public IAsyncRelayCommand ApplyLightThemeCommand => ApplicationShell.ApplyLightThemeCommand;

    public IAsyncRelayCommand ApplyCursorLikeThemeCommand => ApplicationShell.ApplyCursorLikeThemeCommand;

    public IAsyncRelayCommand ApplyPowerClassicThemeCommand => ApplicationShell.ApplyPowerClassicThemeCommand;

    public IRelayCommand<string?> SetUiLanguageCommand => ApplicationShell.SetUiLanguageCommand;

    public IRelayCommand ResetUiLanguageToSystemCommand => ApplicationShell.ResetUiLanguageToSystemCommand;

    public IAsyncRelayCommand OpenThemeFileCommand => ApplicationShell.OpenThemeFileCommand;

    public IRelayCommand DebugStartOrContinueCommand => Debug.DebugStartOrContinueCommand;

    public IRelayCommand DebugAttachCommand => Debug.DebugAttachCommand;

    public IRelayCommand DebugStopCommand => Debug.DebugStopCommand;

    public IRelayCommand DebugStepOverCommand => Debug.DebugStepOverCommand;

    public IRelayCommand DebugStepIntoCommand => Debug.DebugStepIntoCommand;

    public IRelayCommand DebugStepOutCommand => Debug.DebugStepOutCommand;

    public IAsyncRelayCommand BuildSolutionCommand => Build.BuildSolutionCommand;

    public IRelayCommand HideBuildOutputCommand => Build.HideBuildOutputCommand;

    public IRelayCommand TogglePfdRegionExpandedCommand => Shell.TogglePfdRegionExpandedCommand;

    public IRelayCommand ToggleMfdRegionExpandedCommand => Shell.ToggleMfdRegionExpandedCommand;

    public IRelayCommand ToggleBuildOutputCommand => Shell.ToggleBuildOutputCommand;

    public IRelayCommand ToggleTerminalCommand => Shell.ToggleTerminalCommand;

    public IRelayCommand ToggleInstrumentationDockCommand => Shell.ToggleInstrumentationDockCommand;

    public IRelayCommand SetSingleEditorGroupCommand => Shell.SetSingleEditorGroupCommand;

    public IRelayCommand SetDualEditorGroupCommand => Shell.SetDualEditorGroupCommand;

    public IRelayCommand SetTripleEditorGroupCommand => Shell.SetTripleEditorGroupCommand;

    public IRelayCommand ShowPfdRegionPanelCommand => Shell.ShowPfdRegionPanelCommand;

    public IRelayCommand ShowBuildOutputPanelCommand => Shell.ShowBuildOutputPanelCommand;

    public IRelayCommand ShowChatPageCommand => Shell.ShowChatPageCommand;

    public IRelayCommand ShowSolutionExplorerPageCommand => Shell.ShowSolutionExplorerPageCommand;

    public IRelayCommand ShowRelatedFilesMfdPageCommand => Shell.ShowRelatedFilesMfdPageCommand;

    public IRelayCommand ShowTerminalPanelCommand => Shell.ShowTerminalPanelCommand;

    public IRelayCommand<string?> SetUiModeByIdCommand => Shell.SetUiModeByIdCommand;

    public IRelayCommand<object?> SetUiModeByIndexCommand => Shell.SetUiModeByIndexCommand;

    public IRelayCommand CycleUiModeCommand => Shell.CycleUiModeCommand;

    public IRelayCommand<string?> ActivateDocumentCommand => Documents.ActivateDocumentCommand;

    public IRelayCommand<string?> CloseDocumentCommand => Documents.CloseDocumentCommand;

    public IRelayCommand<string?> TogglePinDocumentCommand => Documents.TogglePinDocumentCommand;

    public IRelayCommand<string?> MoveDocumentToGroup1Command => Documents.MoveDocumentToGroup1Command;

    public IRelayCommand<string?> MoveDocumentToGroup2Command => Documents.MoveDocumentToGroup2Command;

    public IRelayCommand<string?> MoveDocumentToGroup3Command => Documents.MoveDocumentToGroup3Command;

    public IRelayCommand ReopenClosedDocumentCommand => Documents.ReopenClosedDocumentCommand;

    public IRelayCommand SetSafetyL1Command => Shell.SetSafetyL1Command;

    public IRelayCommand SetSafetyL2Command => Shell.SetSafetyL2Command;

    public IRelayCommand SetSafetyL3Command => Shell.SetSafetyL3Command;

    public IRelayCommand ShowMarkdownPreviewPageCommand => Shell.ShowMarkdownPreviewPageCommand;

    public IRelayCommand OpenPreviewWindowCommand => Shell.OpenPreviewWindowCommand;
}
