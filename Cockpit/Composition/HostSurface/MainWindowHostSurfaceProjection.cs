using CascadeIDE.Cockpit.Composition.Shell;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Проекция состояния <see cref="IMainWindowHostSurfaceInput"/> во вход shell/host композиторов (ADR 0036 п.3, 0047).
/// Числа ширин MFD из UI-режима передаёт вызывающий (граница Cockpit / <c>Features.UiChrome</c>, CASCOPE002).
/// </summary>
public static class MainWindowHostSurfaceProjection
{
    public static MainWindowHostSurfaceFrame ComposeFrame(
        IMainWindowHostSurfaceInput host,
        int expandedMfdWidthPixels,
        int collapsedMfdWidthPixels) =>
        MainWindowHostSurfaceCompositor.ComposeFrame(
            BuildShellInput(host, expandedMfdWidthPixels, collapsedMfdWidthPixels));

    public static MainWindowShellSurfaceCompositionInput BuildShellInput(
        IMainWindowHostSurfaceInput host,
        int expandedMfdWidthPixels,
        int collapsedMfdWidthPixels) =>
        new(
            host.PresentationParse,
            host.IsPfdRegionExpanded,
            host.IsMfdRegionExpanded,
            host.IsPfdHostWindowShellOpen,
            host.IsMfdHostWindowShellOpen,
            expandedMfdWidthPixels,
            collapsedMfdWidthPixels,
            host.DisplaySettings,
            host.SafetyLevel);
}
