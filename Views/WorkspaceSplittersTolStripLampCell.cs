using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CascadeIDE.Cockpit.PrimitivesKit;

namespace CascadeIDE.Views;

/// <summary>
/// Лампа TOL (Take Off / Landing) для блокировки сплиттеров: тот же корпус и композиция, что у
/// <see cref="CommandArmedStripLampCell"/> (ADR 0064), отрисовка через
/// <see cref="WorkspaceSplittersTolLampFace"/>.
/// </summary>
public sealed class WorkspaceSplittersTolStripLampCell : Control
{
    public static readonly StyledProperty<bool> SplittersLockedProperty =
        AvaloniaProperty.Register<WorkspaceSplittersTolStripLampCell, bool>(nameof(SplittersLocked));

    static WorkspaceSplittersTolStripLampCell()
    {
        AffectsRender<WorkspaceSplittersTolStripLampCell>(SplittersLockedProperty);
        FocusableProperty.OverrideDefaultValue<WorkspaceSplittersTolStripLampCell>(false);
    }

    public bool SplittersLocked
    {
        get => GetValue(SplittersLockedProperty);
        set => SetValue(SplittersLockedProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(
            AnnunciatorLampMetrics.DefaultCellWidth + 4,
            AnnunciatorLampMetrics.DefaultCellHeight + 4);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return;

        var cell = AnnunciatorLampMetrics.DefaultCellWidth;
        var x = (w - cell) / 2;
        var y = (h - cell) / 2;
        var outer = new Rect(x, y, cell, cell);
        new WorkspaceSplittersTolLampFace(SplittersLocked).Draw(context, outer);
    }
}
