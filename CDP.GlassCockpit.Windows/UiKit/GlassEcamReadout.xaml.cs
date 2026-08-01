#nullable enable

using System.Windows;
using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows.UiKit;

/// <summary>WPF adapt of Avalonia EcamReadout — label / value / optional sub.</summary>
public partial class GlassEcamReadout : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(GlassEcamReadout),
            new PropertyMetadata("", OnTextChanged));

    public static readonly DependencyProperty ValueTextProperty =
        DependencyProperty.Register(nameof(ValueText), typeof(string), typeof(GlassEcamReadout),
            new PropertyMetadata("", OnTextChanged));

    public static readonly DependencyProperty SubTextProperty =
        DependencyProperty.Register(nameof(SubText), typeof(string), typeof(GlassEcamReadout),
            new PropertyMetadata(null, OnTextChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public string? SubText
    {
        get => (string?)GetValue(SubTextProperty);
        set => SetValue(SubTextProperty, value);
    }

    public GlassEcamReadout()
    {
        InitializeComponent();
        Loaded += (_, _) => Sync();
    }

    static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassEcamReadout r)
            r.Sync();
    }

    void Sync()
    {
        if (LabelBlock is null)
            return;
        LabelBlock.Text = Label ?? "";
        ValueBlock.Text = ValueText ?? "";
        if (string.IsNullOrWhiteSpace(SubText))
        {
            SubBlock.Text = "";
            SubBlock.Visibility = Visibility.Collapsed;
        }
        else
        {
            SubBlock.Text = SubText;
            SubBlock.Visibility = Visibility.Visible;
        }
    }
}
