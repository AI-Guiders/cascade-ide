#nullable enable

using CascadeIDE.Models;
using CascadeIDE.Models.Shell;

namespace CascadeIDE.Features.Cockpit.Application;

/// <summary>Политика host presentation Cockpit Command Line (ADR 0120).</summary>
public static class CockpitCommandLineHostPolicy
{
    public static bool ShouldShowIntercomSkia(
        bool isCockpitCommandLineOpen,
        CockpitCommandLineHostKind activeHost) =>
        isCockpitCommandLineOpen && activeHost == CockpitCommandLineHostKind.Intercom;

    public static bool ShouldShowEditorOverlay(
        bool isCockpitCommandLineOpen,
        CockpitCommandLineHostKind activeHost,
        PrimaryWorkSurfaceKind primaryWorkSurface,
        CommandPaletteHost commandPaletteHost,
        CommandPaletteHost currentTopLevelHost) =>
        primaryWorkSurface == PrimaryWorkSurfaceKind.Editor
        && isCockpitCommandLineOpen
        && activeHost == CockpitCommandLineHostKind.Editor
        && commandPaletteHost == currentTopLevelHost;
}
