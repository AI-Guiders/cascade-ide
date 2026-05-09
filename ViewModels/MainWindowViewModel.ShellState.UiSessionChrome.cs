using System.Collections.ObjectModel;
using CascadeIDE.Models.Shell;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Часть <see cref="MainWindowViewModel"/>: режим UI, прогресс сборки на полосе, палитра, снимок раскладки.</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    private CommandPaletteHost _commandPaletteHost = CommandPaletteHost.MainWindow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UiModeFamily))]
    [NotifyPropertyChangedFor(nameof(MfdRegionPixelWidth))]
    [NotifyPropertyChangedFor(nameof(IsMfdRegionVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdColumnVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZoneGeometryOverlayPfdVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZoneGeometryOverlayForwardVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZoneGeometryOverlayMfdVisible))]
    [NotifyPropertyChangedFor(nameof(UseSkiaInstrumentMount))]
    [NotifyPropertyChangedFor(nameof(IsPfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdHostWindowIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(ShowTaskBar))]
    [NotifyPropertyChangedFor(nameof(ShowIdeHealthStrip))]
    [NotifyPropertyChangedFor(nameof(ShowIdeHealthMfdPage))]
    [NotifyPropertyChangedFor(nameof(ShowEicasAlertsBar))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(IdeHealthStripSurface))]
    [NotifyPropertyChangedFor(nameof(IdeHealthContentRepresentation))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    [NotifyPropertyChangedFor(nameof(QuickActions))]
    [NotifyPropertyChangedFor(nameof(ShowAgentOperations))]
    [NotifyPropertyChangedFor(nameof(AgentTrace))]
    [NotifyPropertyChangedFor(nameof(AutonomousAgentTelemetry))]
    [NotifyPropertyChangedFor(nameof(IdeHealthOnTerminalTab))]
    [NotifyPropertyChangedFor(nameof(ShowSafetyControls))]
    [NotifyPropertyChangedFor(nameof(ShowTelemetryHiddenHint))]
    [NotifyPropertyChangedFor(nameof(TelemetryButtonText))]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup2))]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup3))]
    [NotifyPropertyChangedFor(nameof(IsRiskCardVisible))]
    [NotifyPropertyChangedFor(nameof(IsResultCardVisible))]
    [NotifyPropertyChangedFor(nameof(AgentOperationsPanel))]
    [NotifyPropertyChangedFor(nameof(InstrumentationTabs))]
    [NotifyPropertyChangedFor(nameof(HypothesesTab))]
    [NotifyPropertyChangedFor(nameof(ShowInstrumentationLayoutMenu))]
    [NotifyPropertyChangedFor(nameof(IsMfdContourContentVisible))]
    [NotifyPropertyChangedFor(nameof(IsProblemsPanelVisible))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string _uiMode = "Balanced";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup2))]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup3))]
    private int _editorGroupCount = 1;

    /// <summary>Снимок раскладки UI (JSON), полоса Workspace Health в Power.</summary>
    [ObservableProperty]
    private string _workspaceSnapshotJson = "";

    public ObservableCollection<FocusPlanItemViewModel> FocusPlanItems { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuildSolutionCommand))]
    [NotifyPropertyChangedFor(nameof(IdeHealthBuildText))]
    [NotifyPropertyChangedFor(nameof(IdeHealthBuildCockpitShort))]
    [NotifyPropertyChangedFor(nameof(IdeHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    private bool _isBuilding;
}
