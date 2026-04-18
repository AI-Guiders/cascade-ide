using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

/// <summary>
/// Полоса ламп annunciator / Korry по коллекции <see cref="AnnunciatorLampItem"/>; отрисовка через <see cref="DrawingContext"/>,
/// тот же контур, что <see cref="CockpitSkiaSceneRenderer"/> и <see cref="SkiaHost"/> (ADR 0055, 0063).
/// Без раздувания под доступную высоту — фиксированная геометрия ячеек.
/// </summary>
public sealed class AnnunciatorLampStrip : Control
{
    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<AnnunciatorLampStrip, IEnumerable?>(nameof(Items));

    private INotifyCollectionChanged? _collectionSubscription;
    private readonly List<(Rect Rect, AnnunciatorLampItem Item)> _hitCells = [];
    private AnnunciatorLampItem? _hovered;

    static AnnunciatorLampStrip()
    {
        AffectsRender<AnnunciatorLampStrip>(ItemsProperty);
        FocusableProperty.OverrideDefaultValue<AnnunciatorLampStrip>(false);
    }

    public IEnumerable? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != ItemsProperty)
            return;
        UnsubscribeCollection();
        if (change.NewValue is INotifyCollectionChanged n)
        {
            n.CollectionChanged += OnCollectionChanged;
            _collectionSubscription = n;
        }

        InvalidateMeasure();
        InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        UnsubscribeCollection();
    }

    private void UnsubscribeCollection()
    {
        if (_collectionSubscription is not null)
        {
            _collectionSubscription.CollectionChanged -= OnCollectionChanged;
            _collectionSubscription = null;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatcher.UIThread.Post(
            () =>
            {
                // Сначала часто приходит measure при пустой коллекции → (0,0). Без InvalidateMeasure размер не обновится, когда строки добавят асинхронно.
                InvalidateMeasure();
                InvalidateVisual();
            },
            DispatcherPriority.Normal);

    protected override Size MeasureOverride(Size availableSize)
    {
        var items = EnumerateItems().ToList();
        if (items.Count == 0)
            return new Size(0, 0);

        var sz = AnnunciatorLampPrimitives.MeasureStrip(items.Count);
        var aw = availableSize.Width;
        if (double.IsNaN(aw) || double.IsInfinity(aw))
            aw = sz.Width;
        // Родитель может отдать ширину 0 на раннем measure — не схлопываемся до нуля, иначе Render не рисует.
        if (aw <= 0)
            aw = sz.Width;
        var width = Math.Min(sz.Width, aw);
        return new Size(width, sz.Height);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _hitCells.Clear();

        var items = EnumerateItems().ToList();
        if (items.Count == 0)
            return;

        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return;

        var panel = new Rect(0, 0, w, h);
        AnnunciatorLampPrimitives.DrawPanelBackground(context, panel);

        var columnsPerRow = AnnunciatorLampPrimitives.DefaultStripColumns;
        var pad = AnnunciatorLampPrimitives.DefaultPanelPadding;
        var gap = AnnunciatorLampPrimitives.DefaultGap;
        var cellW = AnnunciatorLampPrimitives.DefaultCellWidth;
        var cellH = AnnunciatorLampPrimitives.DefaultCellHeight;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var col = i % columnsPerRow;
            var row = i / columnsPerRow;
            var x = pad + col * (cellW + gap);
            var y = pad + row * (cellH + gap);
            var outer = new Rect(x, y, cellW, cellH);

            AnnunciatorLampPrimitives.DrawLampCell(
                context,
                outer,
                item.LampShortLabel,
                item.Level);

            _hitCells.Add((outer, item));
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var p = e.GetPosition(this);
        AnnunciatorLampItem? hit = null;
        foreach (var (r, it) in _hitCells)
        {
            if (r.Contains(p))
            {
                hit = it;
                break;
            }
        }

        if (string.Equals(_hovered?.Id, hit?.Id, StringComparison.Ordinal))
            return;

        _hovered = hit;
        if (hit is null)
        {
            ToolTip.SetTip(this, null);
            return;
        }

        var tip = new StackPanel { Spacing = 4, MaxWidth = 420 };
        tip.Children.Add(new TextBlock { Text = hit.Title, FontWeight = FontWeight.SemiBold, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        tip.Children.Add(new TextBlock
        {
            Text = hit.Detail,
            FontSize = 11,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Foreground = new SolidColorBrush(CockpitPrimitivesPalette.Annunciator.TooltipDetailForeground),
        });
        ToolTip.SetTip(this, tip);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovered = null;
        ToolTip.SetTip(this, null);
    }

    private IEnumerable<AnnunciatorLampItem> EnumerateItems()
    {
        if (Items is null)
            yield break;
        foreach (var o in Items)
        {
            if (o is AnnunciatorLampItem it)
                yield return it;
        }
    }

}
