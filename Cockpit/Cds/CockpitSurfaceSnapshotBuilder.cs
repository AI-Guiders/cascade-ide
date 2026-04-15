using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.Cds;

/// <summary>
/// Сборка CDS-снимка (ADR 0036 п.2) из публичного состояния <see cref="MainWindowViewModel"/>; ортогонально
/// <see cref="UiLayoutSnapshot"/> (дерево UI, п.4) и композиторам каналов (<see cref="WorkspaceHealthSegmentBuilder"/>, <see cref="EicasMessageSorter"/>, п.3).
/// </summary>
public static class CockpitSurfaceSnapshotBuilder
{
    public const string CurrentSchemaVersion = "0.3";

    public static CockpitSurfaceState Build(MainWindowViewModel vm)
    {
        var surfaceKind = ToCdsSurfaceKind(vm.ActiveAttentionLayoutSurface);
        var layout = CockpitPresentationLayoutPolicy.InvariantsFromPresentation(vm.PresentationParse);
        var instruments = vm.MainWindowSurfaceSlotBindings
            .Select(static i => new CockpitSurfaceInstrument(i.InstrumentId, i.SlotId, i.SchemaVersion))
            .ToArray();
        return new CockpitSurfaceState(
            SchemaVersion: CurrentSchemaVersion,
            UiMode: vm.UiMode,
            PresentationEffectiveLine: vm.EffectivePresentationLine,
            PresentationParseSuccess: vm.PresentationParse.IsSuccess,
            Topology: new CockpitSurfaceTopology(
                SurfaceKind: surfaceKind,
                MfdHostWindowOpen: vm.IsMfdHostWindowShellOpen,
                MfdColumnVisibleInMain: vm.IsMfdColumnVisible),
            SecondaryShell: new CockpitSurfaceSecondaryShell(
                CurrentPage: vm.CurrentSecondaryShellPage.ToString()),
            Zones: new CockpitSurfaceZones(
                PfdVisible: vm.IsPfdColumnVisible,
                ForwardVisible: vm.IsForwardZoneVisible,
                MfdVisible: vm.IsMfdColumnVisible,
                PfdRequiredByPresentation: layout.PfdRequiredByPresentation,
                ForwardRequiredByPresentation: layout.ForwardRequiredByPresentation,
                MfdRequiredByPresentation: layout.MfdRequiredByPresentation),
            Instruments: instruments);
    }

    internal static string ToCdsSurfaceKind(AttentionLayoutSurfaceKind kind) =>
        kind switch
        {
            AttentionLayoutSurfaceKind.MainWindowDockedGrid => "main_window_docked_grid",
            AttentionLayoutSurfaceKind.MainWindowPlusMfdHostTopLevel => "main_window_plus_mfd_host_top_level",
            _ => kind.ToString(),
        };
}
