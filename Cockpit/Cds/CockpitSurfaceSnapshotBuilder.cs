using CascadeIDE.Cockpit.Composition;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.WorkspaceHealth;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.Cds;

/// <summary>
/// Сборка CDS-снимка (ADR 0036 п.2) из публичного состояния <see cref="MainWindowViewModel"/>; ортогонально
/// <see cref="UiLayoutSnapshot"/> (дерево UI, п.4) и композиторам каналов (<see cref="WorkspaceHealthSurfaceCompositor"/>, <see cref="EicasMessageSorter"/>, п.3).
/// </summary>
public static class CockpitSurfaceSnapshotBuilder
{
    public const string CurrentSchemaVersion = "0.3";

    public static CockpitSurfaceState Build(MainWindowViewModel vm)
    {
        var surfaceKind = ToCdsSurfaceKind(vm.ActiveAttentionLayoutSurface);
        var layout = CockpitPresentationLayoutPolicy.InvariantsFromPresentation(vm.PresentationParse);
        var instruments = vm.MainWindowHostSurfaceInstruments
            .Select(static (CockpitInstrumentDescriptor i) => new CockpitSurfaceInstrument(i.InstrumentId, i.SlotId, i.SchemaVersion))
            .ToArray();
        return new CockpitSurfaceState(
            SchemaVersion: CurrentSchemaVersion,
            UiMode: vm.UiMode,
            PresentationEffectiveLine: vm.EffectivePresentationLine,
            PresentationParseSuccess: vm.PresentationParse.IsSuccess,
            Topology: new CockpitSurfaceTopology(
                SurfaceKind: surfaceKind,
                MfdHostWindowOpen: vm.IsMfdHostWindowShellOpen,
                PfdHostWindowOpen: vm.IsPfdHostWindowShellOpen,
                MfdColumnVisibleInMain: vm.IsMfdColumnVisible),
            MfdShell: new CockpitSurfaceMfdShell(
                CurrentPage: vm.CurrentMfdShellPage.ToString()),
            Zones: new CockpitSurfaceZones(
                PfdVisible: vm.IsPfdColumnVisible,
                ForwardVisible: true,
                MfdVisible: vm.IsMfdColumnVisible,
                PfdRequiredByPresentation: layout.PfdRequiredByPresentation,
                ForwardRequiredByPresentation: layout.ForwardRequiredByPresentation,
                MfdRequiredByPresentation: layout.MfdRequiredByPresentation),
            Instruments: instruments);
    }

    internal static string ToCdsSurfaceKind(AttentionLayoutSurfaceKind kind) =>
        kind switch
        {
            AttentionLayoutSurfaceKind.MainWindowDockedGrid => MainWindowHostSurfaceIds.DockedGrid,
            AttentionLayoutSurfaceKind.MainWindowPlusMfdHostTopLevel => MainWindowHostSurfaceIds.PlusMfdHostTopLevel,
            AttentionLayoutSurfaceKind.MainWindowPlusPfdHostTopLevel => MainWindowHostSurfaceIds.PlusPfdHostTopLevel,
            AttentionLayoutSurfaceKind.MainWindowPlusPfdMfdHostTopLevel => MainWindowHostSurfaceIds.PlusPfdMfdHostTopLevel,
            _ => kind.ToString(),
        };
}
