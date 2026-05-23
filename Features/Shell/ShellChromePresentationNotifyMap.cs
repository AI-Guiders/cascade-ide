using System.Collections.Frozen;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Shell;

/// <summary>
/// Presentation-зависимости на <see cref="MainWindowViewModel"/> при смене shell-свойств (бывшие <c>NotifyPropertyChangedFor</c> на MWVM).
/// </summary>
internal static class ShellChromePresentationNotifyMap
{
    private static readonly FrozenDictionary<string, string[]> Map = CreateMap();

    public static ReadOnlySpan<string> GetDependents(string shellPropertyName) =>
        Map.TryGetValue(shellPropertyName, out var names) ? names : ReadOnlySpan<string>.Empty;

    private static FrozenDictionary<string, string[]> CreateMap()
    {
        return new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(ShellChromeViewModel.IsMfdRegionExpanded)] =
            [
                nameof(MainWindowViewModel.MfdRegionToggleButtonText),
                nameof(MainWindowViewModel.MfdRegionPixelWidth),
                nameof(MainWindowViewModel.IsMfdRegionVisible),
                nameof(MainWindowViewModel.IsMfdColumnVisible),
                nameof(MainWindowViewModel.IsSkiaZoneGeometryOverlayMfdVisible),
                nameof(MainWindowViewModel.IsPfdIdeHealthMountVisible),
                nameof(MainWindowViewModel.IsMfdIdeHealthMountVisible),
                nameof(MainWindowViewModel.PfdIdeHealthMountContext),
                nameof(MainWindowViewModel.MfdIdeHealthMountContext),
                nameof(MainWindowViewModel.IsMfdRegionCollapsed),
                nameof(MainWindowViewModel.IsChatPanelHidden),
            ],
            [nameof(ShellChromeViewModel.IsPfdRegionExpanded)] =
            [
                nameof(MainWindowViewModel.IsPfdColumnVisible),
                nameof(MainWindowViewModel.IsSkiaZoneGeometryOverlayPfdVisible),
                nameof(MainWindowViewModel.IsPfdIdeHealthMountVisible),
                nameof(MainWindowViewModel.PfdIdeHealthMountContext),
                nameof(MainWindowViewModel.IsPfdRegionCollapsed),
                nameof(MainWindowViewModel.IsSolutionPanelHidden),
            ],
            [nameof(ShellChromeViewModel.IsTerminalVisible)] =
            [
                nameof(MainWindowViewModel.ShowTelemetryHiddenHint),
                nameof(MainWindowViewModel.TelemetryButtonText),
                nameof(MainWindowViewModel.IsMfdContourContentVisible),
                nameof(MainWindowViewModel.ShowWorkspaceBottomChrome),
                nameof(MainWindowViewModel.IsTerminalPanelHidden),
            ],
            [nameof(ShellChromeViewModel.IsGitPanelVisible)] =
            [
                nameof(MainWindowViewModel.IsMfdContourContentVisible),
                nameof(MainWindowViewModel.ShowWorkspaceBottomChrome),
            ],
            [nameof(ShellChromeViewModel.IsBuildOutputVisible)] =
            [
                nameof(MainWindowViewModel.IsMfdContourContentVisible),
                nameof(MainWindowViewModel.ShowWorkspaceBottomChrome),
                nameof(MainWindowViewModel.IsBuildPanelHidden),
            ],
            [nameof(ShellChromeViewModel.IsInstrumentationDockVisible)] =
            [
                nameof(MainWindowViewModel.InstrumentationTabs),
                nameof(MainWindowViewModel.HypothesesTab),
                nameof(MainWindowViewModel.IsMfdContourContentVisible),
                nameof(MainWindowViewModel.ShowWorkspaceBottomChrome),
            ],
            [nameof(ShellChromeViewModel.CurrentMfdShellPage)] =
            [
                nameof(MainWindowViewModel.IsMfdShellSolutionExplorerPageActive),
                nameof(MainWindowViewModel.CurrentMfdShellPageAsShell),
                nameof(MainWindowViewModel.ChatPanelColumnPixelWidth),
                nameof(MainWindowViewModel.IsChatPanelColumnVisible),
                nameof(MainWindowViewModel.IsMfdColumnVisible),
                nameof(MainWindowViewModel.MfdRegionPixelWidth),
                nameof(MainWindowViewModel.IsMfdRegionVisible),
                nameof(MainWindowViewModel.MainGridColumnDefinitions),
            ],
            [nameof(ShellChromeViewModel.UiMode)] =
            [
                nameof(MainWindowViewModel.UiModeFamily),
                nameof(MainWindowViewModel.MfdRegionPixelWidth),
                nameof(MainWindowViewModel.IsMfdRegionVisible),
                nameof(MainWindowViewModel.IsMfdColumnVisible),
                nameof(MainWindowViewModel.IsSkiaZoneGeometryOverlayPfdVisible),
                nameof(MainWindowViewModel.IsSkiaZoneGeometryOverlayForwardVisible),
                nameof(MainWindowViewModel.IsSkiaZoneGeometryOverlayMfdVisible),
                nameof(MainWindowViewModel.UseSkiaInstrumentMount),
                nameof(MainWindowViewModel.IsPfdIdeHealthMountVisible),
                nameof(MainWindowViewModel.IsMfdIdeHealthMountVisible),
                nameof(MainWindowViewModel.IsMfdHostWindowIdeHealthMountVisible),
                nameof(MainWindowViewModel.PfdIdeHealthMountContext),
                nameof(MainWindowViewModel.MfdIdeHealthMountContext),
                nameof(MainWindowViewModel.ShowTaskBar),
                nameof(MainWindowViewModel.ShowIdeHealthStrip),
                nameof(MainWindowViewModel.ShowIdeHealthMfdPage),
                nameof(MainWindowViewModel.ShowEicasAlertsBar),
                nameof(MainWindowViewModel.ShowWorkspaceChromeBand),
                nameof(MainWindowViewModel.IdeHealthStripSurface),
                nameof(MainWindowViewModel.IdeHealthContentRepresentation),
                nameof(MainWindowViewModel.ShowWorkspaceBottomChrome),
                nameof(MainWindowViewModel.QuickActions),
                nameof(MainWindowViewModel.ShowAgentOperations),
                nameof(MainWindowViewModel.AgentTrace),
                nameof(MainWindowViewModel.AutonomousAgentTelemetry),
                nameof(MainWindowViewModel.IdeHealthOnTerminalTab),
                nameof(MainWindowViewModel.ShowSafetyControls),
                nameof(MainWindowViewModel.ShowTelemetryHiddenHint),
                nameof(MainWindowViewModel.TelemetryButtonText),
                nameof(MainWindowViewModel.ShowEditorGroup2),
                nameof(MainWindowViewModel.ShowEditorGroup3),
                nameof(MainWindowViewModel.IsRiskCardVisible),
                nameof(MainWindowViewModel.IsResultCardVisible),
                nameof(MainWindowViewModel.AgentOperationsPanel),
                nameof(MainWindowViewModel.InstrumentationTabs),
                nameof(MainWindowViewModel.HypothesesTab),
                nameof(MainWindowViewModel.ShowInstrumentationLayoutMenu),
                nameof(MainWindowViewModel.IsMfdContourContentVisible),
                nameof(MainWindowViewModel.IsProblemsPanelVisible),
                nameof(MainWindowViewModel.WindowTitle),
            ],
            [nameof(ShellChromeViewModel.EditorGroupCount)] =
            [
                nameof(MainWindowViewModel.ShowEditorGroup2),
                nameof(MainWindowViewModel.ShowEditorGroup3),
            ],
            [nameof(ShellChromeViewModel.IsBuilding)] =
            [
                nameof(MainWindowViewModel.IdeHealthBuildText),
                nameof(MainWindowViewModel.IdeHealthBuildCockpitShort),
                nameof(MainWindowViewModel.IdeHealthMountPayload),
                nameof(MainWindowViewModel.PfdIdeHealthMountContext),
                nameof(MainWindowViewModel.MfdIdeHealthMountContext),
            ],
        }.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
