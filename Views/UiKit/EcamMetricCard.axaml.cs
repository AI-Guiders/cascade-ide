using Avalonia;
using Avalonia.Controls;

namespace CascadeIDE.Views.UiKit;

public partial class EcamMetricCard : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<EcamMetricCard, string?>(nameof(Title));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public EcamMetricCard()
    {
        InitializeComponent();
    }
}

