using System.ComponentModel;
using Avalonia.Controls;

namespace CascadeIDE.Views;

public partial class MainWindow
{
    private CockpitSkiaSceneRenderer? _mainWindowSkiaRenderer;

    private void AttachSkiaHostRenderers()
    {
        _mainWindowSkiaRenderer ??= new CockpitSkiaSceneRenderer(
            () => DataContext as ViewModels.MainWindowViewModel,
            SkiaHostSurface.MainWindow);

        if (this.FindControl<SkiaHost>("PfdSkiaHost") is { } pfd)
            pfd.Renderer = _mainWindowSkiaRenderer;
        if (this.FindControl<SkiaHost>("ForwardSkiaHost") is { } forward)
            forward.Renderer = _mainWindowSkiaRenderer;
        if (this.FindControl<SkiaHost>("MfdSkiaHost") is { } mfd)
            mfd.Renderer = _mainWindowSkiaRenderer;
    }

    private void InvalidateSkiaHosts()
    {
        if (this.FindControl<SkiaHost>("PfdSkiaHost") is { } pfd)
            pfd.InvalidateVisual();
        if (this.FindControl<SkiaHost>("ForwardSkiaHost") is { } forward)
            forward.InvalidateVisual();
        if (this.FindControl<SkiaHost>("MfdSkiaHost") is { } mfd)
            mfd.InvalidateVisual();
    }

    internal static bool IsSkiaHostRelatedProperty(string? propertyName) =>
        propertyName is nameof(ViewModels.MainWindowViewModel.ShowSkiaZoneGeometryOverlay)
            or nameof(ViewModels.MainWindowViewModel.IsSkiaZoneGeometryOverlayPfdVisible)
            or nameof(ViewModels.MainWindowViewModel.IsSkiaZoneGeometryOverlayForwardVisible)
            or nameof(ViewModels.MainWindowViewModel.IsSkiaZoneGeometryOverlayMfdVisible)
            or nameof(ViewModels.MainWindowViewModel.IsPfdColumnVisible)
            or nameof(ViewModels.MainWindowViewModel.IsMfdColumnVisible)
            or nameof(ViewModels.MainWindowViewModel.MainGridColumnDefinitions)
            or nameof(ViewModels.MainWindowViewModel.IsPfdRegionExpanded)
            or nameof(ViewModels.MainWindowViewModel.WorkspaceNavigationMapRelatedCount)
            or nameof(ViewModels.MainWindowViewModel.WorkspaceNavigationMapHasRelated)
            or nameof(ViewModels.MainWindowViewModel.CodeNavigationMapGraphScene);
}
