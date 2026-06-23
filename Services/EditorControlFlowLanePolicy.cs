using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services;

/// <summary>Control-flow virtual spacing policy (ADR 0152); Monaco uses glyphs, lane constants for future layout.</summary>
public static class EditorControlFlowLanePolicy
{
    public const double GlyphRadius = 6.2;
    public const double LanePadding = 3.0;
    public const double LaneWidthPixels = GlyphRadius * 2 + LanePadding * 2;
    public const double LaneHalfWidth = LaneWidthPixels / 2;

    public static bool ShouldReserveLane(
        string? codeNavigationMapLevel,
        string? cfAnchorFullPath,
        string? filePath,
        CodeNavigationMapGraphSceneVm? scene)
    {
        if (!string.Equals(
                CodeNavigationMapLevelKind.Normalize(codeNavigationMapLevel),
                CodeNavigationMapLevelKind.ControlFlow,
                StringComparison.Ordinal))
            return false;

        if (string.IsNullOrEmpty(cfAnchorFullPath)
            || string.IsNullOrWhiteSpace(filePath)
            || !EditorTextCoordinateUtilities.PathsReferToSameFile(cfAnchorFullPath, filePath))
            return false;

        return scene is not null
               && !scene.IsEmpty
               && scene.Presentation == CodeNavigationMapGraphPresentationKind.CodeControlFlow;
    }
}
