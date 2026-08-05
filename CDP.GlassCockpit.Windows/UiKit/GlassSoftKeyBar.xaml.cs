#nullable enable

using System.Windows;
using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows.UiKit;

/// <summary>WPF SoftKey bar — modern Glass tokens (not Avalonia ECAM green).</summary>
public partial class GlassSoftKeyBar : UserControl
{
    public static readonly DependencyProperty Key1TextProperty =
        DependencyProperty.Register(nameof(Key1Text), typeof(string), typeof(GlassSoftKeyBar),
            new PropertyMetadata("KEY1", OnTextChanged));

    public static readonly DependencyProperty Key2TextProperty =
        DependencyProperty.Register(nameof(Key2Text), typeof(string), typeof(GlassSoftKeyBar),
            new PropertyMetadata("KEY2", OnTextChanged));

    public static readonly DependencyProperty Key3TextProperty =
        DependencyProperty.Register(nameof(Key3Text), typeof(string), typeof(GlassSoftKeyBar),
            new PropertyMetadata("KEY3", OnTextChanged));

    public string Key1Text
    {
        get => (string)GetValue(Key1TextProperty);
        set => SetValue(Key1TextProperty, value);
    }

    public string Key2Text
    {
        get => (string)GetValue(Key2TextProperty);
        set => SetValue(Key2TextProperty, value);
    }

    public string Key3Text
    {
        get => (string)GetValue(Key3TextProperty);
        set => SetValue(Key3TextProperty, value);
    }

    public event RoutedEventHandler? Key1Click;
    public event RoutedEventHandler? Key2Click;
    public event RoutedEventHandler? Key3Click;

    public GlassSoftKeyBar()
    {
        InitializeComponent();
        Sync();
        Loaded += (_, _) => Sync();
    }

    static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassSoftKeyBar bar)
            bar.Sync();
    }

    void Sync()
    {
        if (Key1 is null)
            return;
        Key1.Content = Key1Text ?? "";
        Key2.Content = Key2Text ?? "";
        Key3.Content = Key3Text ?? "";
    }

    void OnKey1Click(object sender, RoutedEventArgs e) => Key1Click?.Invoke(this, e);
    void OnKey2Click(object sender, RoutedEventArgs e) => Key2Click?.Invoke(this, e);
    void OnKey3Click(object sender, RoutedEventArgs e) => Key3Click?.Invoke(this, e);
}
