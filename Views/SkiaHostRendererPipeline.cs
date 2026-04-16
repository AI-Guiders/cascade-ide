using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views;

/// <summary>
/// Slot-aware scene renderer bound to runtime VM/CDS state.
/// Visual output is intentionally minimal at this stage; the class primarily
/// wires SkiaHost to real slot topology/state and pointer routing.
/// </summary>
public sealed class CockpitSkiaSceneRenderer : ISkiaSceneRenderer
{
    private readonly Func<MainWindowViewModel?> _vmProvider;
    private readonly SkiaHostSurface _surface;

    public CockpitSkiaSceneRenderer(Func<MainWindowViewModel?> vmProvider, SkiaHostSurface surface)
    {
        _vmProvider = vmProvider;
        _surface = surface;
    }

    public void Render(DrawingContext context, Rect bounds, SkiaHostSlot slot)
    {
        var vm = _vmProvider();
        if (vm is null || bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var frame = BuildFrame(vm, slot, _surface, bounds);
        if (!frame.Visible)
            return;

        // Slot visuals: Avalonia controls inside SkiaHost. SM: тонкий accent, когда related непустой (не рисуем список).
        if (slot == SkiaHostSlot.Pfd && _surface == SkiaHostSurface.MainWindow
            && vm.IsPfdRegionExpanded && vm.WorkspaceNavigationMapHasRelated)
            DrawSemanticMapActiveAccent(context, bounds);

        static void DrawSemanticMapActiveAccent(DrawingContext ctx, Rect b)
        {
            const double w = 3;
            var strip = new Rect(b.Left, b.Top, Math.Min(w, b.Width), b.Height);
            var g = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.Parse("#7CC9FF"), 0),
                    new GradientStop(Color.Parse("#B3A5FF"), 0.55),
                    new GradientStop(Color.FromArgb(0, 179, 165, 255), 1)
                }
            };
            ctx.DrawRectangle(g, null, strip);
        }
    }

    public void OnPointerPressed(Point position, PointerPressedEventArgs e, SkiaHostSlot slot)
    {
    }

    public void OnPointerMoved(Point position, PointerEventArgs e, SkiaHostSlot slot)
    {
    }

    public void OnPointerReleased(Point position, PointerReleasedEventArgs e, SkiaHostSlot slot)
    {
    }

    private static SkiaHostRenderFrame BuildFrame(
        MainWindowViewModel vm,
        SkiaHostSlot slot,
        SkiaHostSurface surface,
        Rect bounds)
    {
        var cds = vm.BuildCockpitSurfaceSnapshot();
        var visible = slot switch
        {
            SkiaHostSlot.Pfd => cds.Zones.PfdVisible,
            SkiaHostSlot.Forward => cds.Zones.ForwardVisible,
            SkiaHostSlot.Mfd => surface == SkiaHostSurface.MfdHostWindow
                ? vm.IsMfdHostWindowShellOpen
                : cds.Zones.MfdVisible,
            _ => true
        };

        return new SkiaHostRenderFrame(
            slot,
            surface,
            cds.Topology.SurfaceKind,
            vm.UiMode,
            vm.SafetyLevel,
            visible,
            bounds);
    }
}

public enum SkiaHostSurface
{
    MainWindow,
    MfdHostWindow
}

public readonly record struct SkiaHostRenderFrame(
    SkiaHostSlot Slot,
    SkiaHostSurface Surface,
    string SurfaceKind,
    string UiMode,
    string SafetyLevel,
    bool Visible,
    Rect Bounds);
