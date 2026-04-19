using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CascadeIDE.Cockpit.PrimitivesKit;

namespace CascadeIDE.Views;

/// <summary>
/// Лампа CascadeChord «CMD + зелёная полоса» (ADR 0060): отрисовка через
/// <see cref="AnnunciatorLampPrimitives.DrawCommandArmedStripLampCell"/> — не дублировать палитру/геометрию в AXAML (ADR 0064/0066).
/// </summary>
public sealed class CommandArmedStripLampCell : Control
{
    public static readonly StyledProperty<bool> IsArmedProperty =
        AvaloniaProperty.Register<CommandArmedStripLampCell, bool>(nameof(IsArmed));

    static CommandArmedStripLampCell()
    {
        AffectsRender<CommandArmedStripLampCell>(IsArmedProperty);
        FocusableProperty.OverrideDefaultValue<CommandArmedStripLampCell>(false);
    }

    public bool IsArmed
    {
        get => GetValue(IsArmedProperty);
        set => SetValue(IsArmedProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(
            AnnunciatorLampPrimitives.DefaultCellWidth + 4,
            AnnunciatorLampPrimitives.DefaultCellHeight + 4);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return;

        var cell = AnnunciatorLampPrimitives.DefaultCellWidth;
        var x = (w - cell) / 2;
        var y = (h - cell) / 2;
        var outer = new Rect(x, y, cell, cell);
        AnnunciatorLampPrimitives.DrawCommandArmedStripLampCell(context, outer, IsArmed);
    }
}
