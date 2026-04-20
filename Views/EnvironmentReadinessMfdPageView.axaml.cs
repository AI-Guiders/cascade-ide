using Avalonia.Controls;
using CascadeIDE.Cockpit.Composition.EnvironmentReadiness;

namespace CascadeIDE.Views;

/// <summary>
/// Готовность окружения: ADR 0063/0068 — одна коллекция payload; проекция «карточки» vs «таблица с лампой в первой колонке»
/// (<see cref="EnvironmentReadinessPresentationResolver"/>).
/// </summary>
public partial class EnvironmentReadinessMfdPageView : UserControl
{
    /// <summary>Минимальная ширина контрола для режима «таблица» (px); синхрон с <see cref="EnvironmentReadinessPresentationResolver.DefaultWideLayoutMinWidthPx"/>.</summary>
    public const double WideLayoutMinWidth = EnvironmentReadinessPresentationResolver.DefaultWideLayoutMinWidthPx;

    public EnvironmentReadinessMfdPageView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        ApplyLayoutForWidth(Bounds.Width);
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        ApplyLayoutForWidth(e.NewSize.Width);
    }

    private void ApplyLayoutForWidth(double width)
    {
        if (WideLayoutRoot is null || CompactLayoutRoot is null)
            return;

        // Пока нет измерения — оставляем компакт (типично узкий MFD).
        if (width <= 0)
            return;

        var kind = EnvironmentReadinessPresentationResolver.Resolve(width);
        WideLayoutRoot.IsVisible = kind == EnvironmentReadinessPresentationKind.WideTable;
        CompactLayoutRoot.IsVisible = kind == EnvironmentReadinessPresentationKind.CompactCards;
    }
}
