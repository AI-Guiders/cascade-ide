using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Проекция состояния <see cref="MainWindowViewModel"/> во вход shell/host композиторов (ADR 0036 п.3, 0047).
/// Числа ширин MFD из UI-режима передаёт VM (граница Cockpit / <c>Features.UiChrome</c>, CASCOPE002).
/// </summary>
public static class MainWindowHostSurfaceProjection
{
    public static MainWindowHostSurfaceFrame ComposeFrame(
        MainWindowViewModel vm,
        int expandedMfdWidthPixels,
        int collapsedMfdWidthPixels) =>
        MainWindowHostSurfaceCompositor.ComposeFrame(BuildShellInput(vm, expandedMfdWidthPixels, collapsedMfdWidthPixels));

    public static MainWindowShellSurfaceCompositionInput BuildShellInput(
        MainWindowViewModel vm,
        int expandedMfdWidthPixels,
        int collapsedMfdWidthPixels) =>
        new(
            vm.PresentationParse,
            vm.IsPfdRegionExpanded,
            vm.IsMfdRegionExpanded,
            vm.IsPfdHostWindowShellOpen,
            vm.IsMfdHostWindowShellOpen,
            expandedMfdWidthPixels,
            collapsedMfdWidthPixels,
            vm.DisplaySettings,
            vm.SafetyLevel);
}
