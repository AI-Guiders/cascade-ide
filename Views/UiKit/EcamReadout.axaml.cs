using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CascadeIDE.Views.UiKit;

public partial class EcamReadout : UserControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<EcamReadout, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueTextProperty =
        AvaloniaProperty.Register<EcamReadout, string?>(nameof(ValueText));

    public static readonly StyledProperty<string?> SubTextProperty =
        AvaloniaProperty.Register<EcamReadout, string?>(nameof(SubText));

    public static readonly StyledProperty<double> ValueFontSizeProperty =
        AvaloniaProperty.Register<EcamReadout, double>(nameof(ValueFontSize), defaultValue: 16);

    public static readonly StyledProperty<FontWeight> ValueFontWeightProperty =
        AvaloniaProperty.Register<EcamReadout, FontWeight>(nameof(ValueFontWeight), defaultValue: FontWeight.SemiBold);

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? ValueText
    {
        get => GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public string? SubText
    {
        get => GetValue(SubTextProperty);
        set => SetValue(SubTextProperty, value);
    }

    public double ValueFontSize
    {
        get => GetValue(ValueFontSizeProperty);
        set => SetValue(ValueFontSizeProperty, value);
    }

    public FontWeight ValueFontWeight
    {
        get => GetValue(ValueFontWeightProperty);
        set => SetValue(ValueFontWeightProperty, value);
    }

    public EcamReadout()
    {
        InitializeComponent();
    }
}

