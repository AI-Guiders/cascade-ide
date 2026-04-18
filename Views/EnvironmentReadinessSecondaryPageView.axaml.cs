using Avalonia.Controls;

namespace CascadeIDE.Views;

/// <summary>
/// Готовность окружения: ADR 0063 — полоса ламп (компактный deck) и ниже текстовый deck
/// (карточки при узкой колонке, таблица при ширине ≥ <see cref="WideLayoutMinWidth"/>).
/// </summary>
public partial class EnvironmentReadinessSecondaryPageView : UserControl
{
    /// <summary>Минимальная ширина контрола для режима «таблица» (px).</summary>
    public const double WideLayoutMinWidth = 420;

    public EnvironmentReadinessSecondaryPageView()
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

        var useWide = width >= WideLayoutMinWidth;
        WideLayoutRoot.IsVisible = useWide;
        CompactLayoutRoot.IsVisible = !useWide;
    }
}
