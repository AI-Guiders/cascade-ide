using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

/// <summary>
/// Одна ячейка annunciator / Korry по <see cref="AnnunciatorLampItem"/> (тот же контур, что <see cref="AnnunciatorLampStrip"/>, без фона полосы).
/// Для проекции «лампа в колонке таблицы» (ADR 0068).
/// </summary>
public sealed class AnnunciatorLampCell : Control
{
    public static readonly StyledProperty<AnnunciatorLampItem?> ItemProperty =
        AvaloniaProperty.Register<AnnunciatorLampCell, AnnunciatorLampItem?>(nameof(Item));

    private AnnunciatorLampItem? _hovered;

    static AnnunciatorLampCell()
    {
        AffectsRender<AnnunciatorLampCell>(ItemProperty);
        FocusableProperty.OverrideDefaultValue<AnnunciatorLampCell>(false);
    }

    public AnnunciatorLampItem? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Item is null)
            return new Size(0, 0);

        return new Size(
            AnnunciatorLampMetrics.DefaultCellWidth + 4,
            AnnunciatorLampMetrics.DefaultCellHeight + 4);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var item = Item;
        if (item is null)
            return;

        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return;

        var cell = AnnunciatorLampMetrics.DefaultCellWidth;
        var x = (w - cell) / 2;
        var y = (h - cell) / 2;
        var outer = new Rect(x, y, cell, cell);
        new LabeledAnnunciatorLampFace(item.LampShortLabel, item.Level).Draw(context, outer);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var item = Item;
        if (item is null)
        {
            ClearTip();
            return;
        }

        var p = e.GetPosition(this);
        if (!Bounds.Contains(p))
        {
            ClearTip();
            return;
        }

        if (string.Equals(_hovered?.Id, item.Id, StringComparison.Ordinal))
            return;

        _hovered = item;
        var tip = new StackPanel { Spacing = 4, MaxWidth = 420 };
        tip.Children.Add(new TextBlock { Text = item.Title, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        tip.Children.Add(new TextBlock
        {
            Text = item.Detail,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(CockpitPrimitivesPalette.Annunciator.TooltipDetailForeground),
        });
        ToolTip.SetTip(this, tip);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        ClearTip();
    }

    private void ClearTip()
    {
        _hovered = null;
        ToolTip.SetTip(this, null);
    }
}
