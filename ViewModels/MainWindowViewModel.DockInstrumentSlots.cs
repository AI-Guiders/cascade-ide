using CascadeIDE.Cockpit.Composition.HostSurface;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Какой инструмент показан в слотах PFD/MFD главного окна — по <see cref="InstrumentPlacementRuntime"/> и
/// <see cref="DisplaySettings"/> (в т.ч. <c>[display.instrument_routing]</c> и merge <c>workspace.toml</c>).
/// Логика — <see cref="MainWindowDockedGridInstrumentSlots"/>.
/// </summary>
public partial class MainWindowViewModel
{
    private static readonly string[] DockedInstrumentPlacementBindingNames =
    [
        nameof(IsDockedPfdSolutionExplorerTree),
        nameof(IsDockedPfdWorkspaceNavigationMap),
        nameof(IsDockedMfdSolutionExplorerTree),
        nameof(IsMfdShellSolutionExplorerPageActive),
    ];

    /// <summary>Дерево решения в колонке PFD — только если карта назначает его в слот Pfd.</summary>
    public bool IsDockedPfdSolutionExplorerTree =>
        MainWindowDockedGridInstrumentSlots.IsDockedPfdSolutionExplorerTree(_settings.Display);

    /// <summary>Карта намерений в колонке PFD — если карта назначает workspace map в слот Pfd.</summary>
    public bool IsDockedPfdWorkspaceNavigationMap =>
        MainWindowDockedGridInstrumentSlots.IsDockedPfdWorkspaceNavigationMap(_settings.Display);

    /// <summary>Дерево решения на странице вторичного контура «Обозреватель» — только если карта назначает дерево в слот Mfd (не дублируем PFD).</summary>
    public bool IsDockedMfdSolutionExplorerTree =>
        MainWindowDockedGridInstrumentSlots.IsDockedMfdSolutionExplorerTree(_settings.Display);

    /// <summary>Вызывать после смены решения / merge <c>workspace.toml</c> / настроек Display, влияющих на placement.</summary>
    public void NotifyDockedInstrumentSlotBindings()
    {
        foreach (var name in DockedInstrumentPlacementBindingNames)
            OnPropertyChanged(name);
        NotifyWorkspaceBackgroundStatusStripPlacement();
        CoerceMfdShellPageToAllowed();
    }
}
