using Avalonia.Headless.XUnit;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Регрессия: дерево в колонке PFD и страница «Обозреватель» в MfdShell не должны показываться одновременно из‑за прямого присвоения <see cref="MainWindowViewModel.CurrentMfdShellPage"/>.
/// </summary>
[Collection("UiModeCatalog")]
public sealed class MfdShellSolutionExplorerCoercionTests : IDisposable
{
    public MfdShellSolutionExplorerCoercionTests()
    {
        UiModeCatalog.ResetForTests();
        InstrumentPlacementRuntime.ResetToCodeDefaults();
    }

    public void Dispose()
    {
        InstrumentPlacementRuntime.ResetToCodeDefaults();
        UiModeCatalog.ResetForTests();
    }

    /// <summary>
    /// Сценарий «дерево в PFD», не в MFD. Не полагаемся на <see cref="SettingsService.Load"/>:
    /// в <c>defaults-settings.toml</c> по умолчанию <c>pfd_primary = workspace_map</c>.
    /// </summary>
    private static void ApplyTreeInPfdOnlyRouting(DisplaySettings display)
    {
        display.Instruments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [InstrumentRoutingSlotKeys.PfdPrimary] = CockpitStandardInstrumentIds.SolutionExplorerTree,
        };
    }

    [AvaloniaFact]
    public void Direct_assignment_to_solution_explorer_shell_page_is_coerced_when_tree_is_pfd_docked()
    {
        var vm = new MainWindowViewModel();
        ApplyTreeInPfdOnlyRouting(vm.DisplaySettings);
        vm.NotifyDockedInstrumentSlotBindings();

        Assert.True(vm.IsDockedPfdSolutionExplorerTree);
        Assert.False(vm.IsDockedMfdSolutionExplorerTree);

        vm.CurrentMfdShellPage = MfdShellPage.SolutionExplorer;

        Assert.NotEqual(MfdShellPage.SolutionExplorer, vm.CurrentMfdShellPage);
        Assert.False(vm.IsMfdShellSolutionExplorerPageActive);
    }

    [AvaloniaFact]
    public void Solution_explorer_shell_page_stays_when_tree_is_mfd_docked()
    {
        var vm = new MainWindowViewModel();
        vm.DisplaySettings.Instruments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [InstrumentRoutingSlotKeys.PfdPrimary] = "workspace_map",
            [InstrumentRoutingSlotKeys.MfdPrimary] = "solution_explorer_tree",
        };
        vm.NotifyDockedInstrumentSlotBindings();

        Assert.False(vm.IsDockedPfdSolutionExplorerTree);
        Assert.True(vm.IsDockedMfdSolutionExplorerTree);

        vm.TryNavigateToMfdShellPage(MfdShellPage.SolutionExplorer);

        Assert.Equal(MfdShellPage.SolutionExplorer, vm.CurrentMfdShellPage);
        Assert.True(vm.IsMfdShellSolutionExplorerPageActive);
    }
}
