using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace CascadeIDE.Views;

/// <summary>
/// Slot-scoped host for Skia-backed cockpit rendering.
/// Keeps layout/input participation in the visual tree and provides
/// a single integration point for slot-local rendering/input routing.
/// </summary>
public class SkiaHost : Decorator
{
    public static readonly StyledProperty<SkiaHostSlot> SlotProperty =
        AvaloniaProperty.Register<SkiaHost, SkiaHostSlot>(nameof(Slot), SkiaHostSlot.Forward);

    private ISkiaSceneRenderer? _renderer;

    static SkiaHost()
    {
        AffectsRender<SkiaHost>(SlotProperty);
    }

    public SkiaHostSlot Slot
    {
        get => GetValue(SlotProperty);
        set => SetValue(SlotProperty, value);
    }

    /// <summary>
    /// Slot-local scene renderer. When set, SkiaHost delegates visual drawing and pointer events
    /// to this renderer while remaining a normal Avalonia layout/input participant.
    /// </summary>
    public ISkiaSceneRenderer? Renderer
    {
        get => _renderer;
        set
        {
            if (ReferenceEquals(_renderer, value))
                return;
            _renderer = value;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        _renderer?.Render(context, new Rect(Bounds.Size), Slot);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_renderer is null)
            return;

        var position = e.GetPosition(this);
        _renderer.OnPointerPressed(position, e, Slot);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_renderer is null)
            return;

        var position = e.GetPosition(this);
        _renderer.OnPointerMoved(position, e, Slot);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_renderer is null)
            return;

        var position = e.GetPosition(this);
        _renderer.OnPointerReleased(position, e, Slot);
    }
}

public enum SkiaHostSlot
{
    Pfd,
    Forward,
    Mfd
}

/// <summary>
/// Contract for slot-local Skia scene integration.
/// Implementations may render and handle pointer input for a specific host slot.
/// </summary>
public interface ISkiaSceneRenderer
{
    void Render(DrawingContext context, Rect bounds, SkiaHostSlot slot);

    void OnPointerPressed(Point position, PointerPressedEventArgs e, SkiaHostSlot slot)
    {
    }

    void OnPointerMoved(Point position, PointerEventArgs e, SkiaHostSlot slot)
    {
    }

    void OnPointerReleased(Point position, PointerReleasedEventArgs e, SkiaHostSlot slot)
    {
    }
}
