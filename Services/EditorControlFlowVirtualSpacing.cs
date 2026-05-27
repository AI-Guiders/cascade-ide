using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services;

/// <summary>
/// Резерв ширины слева от текста под глифы control-flow (Virtual Spacing, ADR 0152).
/// </summary>
public static class EditorControlFlowVirtualSpacing
{
    public const double GlyphRadius = 6.2;
    public const double LanePadding = 3.0;

    /// <summary>Полная ширина полосы: круг + отступы.</summary>
    public const double LaneWidthPixels = GlyphRadius * 2 + LanePadding * 2;

    public const double LaneHalfWidth = LaneWidthPixels / 2;

    public static int VisualColumnsForWidth(TextView textView) =>
        VisualColumnsForPixelWidth(textView, LaneWidthPixels);

    public static int VisualColumnsForPixelWidth(TextView textView, double pixelWidth)
    {
        double w = textView.WideSpaceWidth;
        if (w <= 0)
            w = 7.0;
        return Math.Max(1, (int)Math.Ceiling(pixelWidth / w));
    }

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
