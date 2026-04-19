using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace CascadeIDE.Services;

/// <summary>
/// Avalonia 12: у <see cref="TopLevel"/> нет <c>PointerOverElement</c>. Держим последнюю клиентскую позицию
/// по PointerMoved / PointerEntered и вызываем <c>InputHitTest</c> на <see cref="TopLevel"/>.
/// </summary>
public static class UiPointerClientPosition
{
    private static TopLevel? _lastTopLevel;
    private static Point _lastPoint;

    public static void Attach(TopLevel topLevel)
    {
        topLevel.PointerMoved += (_, e) => Record(topLevel, e.GetPosition(topLevel));
        topLevel.PointerEntered += (_, e) => Record(topLevel, e.GetPosition(topLevel));
    }

    private static void Record(TopLevel topLevel, Point clientPoint)
    {
        _lastTopLevel = topLevel;
        _lastPoint = clientPoint;
    }

    public static IInputElement? TryGetPointerOver(TopLevel topLevel)
    {
        if (_lastTopLevel is null || !ReferenceEquals(topLevel, _lastTopLevel))
            return null;
        return topLevel.InputHitTest(_lastPoint);
    }

    /// <summary>Control под курсором в указанном top-level, если последнее событие указателя было в нём.</summary>
    public static Control? TryGetControlUnderPointer(TopLevel topLevel) =>
        ControlFromHit(TryGetPointerOver(topLevel));

    /// <summary>Control под курсором в том окне, куда последним пришёл PointerMoved.</summary>
    public static Control? TryGetPointerOverControlAnywhere()
    {
        if (_lastTopLevel is null)
            return null;
        return ControlFromHit(_lastTopLevel.InputHitTest(_lastPoint));
    }

    /// <summary>Результат хит-теста → ближайший <see cref="Control"/> (сам элемент или предок).</summary>
    public static Control? ControlFromHit(IInputElement? hit) =>
        hit as Control ?? NearestAncestorControl(hit as Visual);

    private static Control? NearestAncestorControl(Visual? visual)
    {
        for (var v = visual?.GetVisualParent(); v is not null; v = v.GetVisualParent())
        {
            if (v is Control c)
                return c;
        }
        return null;
    }
}
