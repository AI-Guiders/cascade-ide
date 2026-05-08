using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CascadeIDE.Views.UiKit;

public partial class EcamSoftKeyBar : UserControl
{
    public static readonly StyledProperty<string> Button1TextProperty =
        AvaloniaProperty.Register<EcamSoftKeyBar, string>(nameof(Button1Text), defaultValue: "BTN1");
    public static readonly StyledProperty<ICommand?> Button1CommandProperty =
        AvaloniaProperty.Register<EcamSoftKeyBar, ICommand?>(nameof(Button1Command));

    public static readonly StyledProperty<string> Button2TextProperty =
        AvaloniaProperty.Register<EcamSoftKeyBar, string>(nameof(Button2Text), defaultValue: "BTN2");
    public static readonly StyledProperty<ICommand?> Button2CommandProperty =
        AvaloniaProperty.Register<EcamSoftKeyBar, ICommand?>(nameof(Button2Command));

    public static readonly StyledProperty<string> Button3TextProperty =
        AvaloniaProperty.Register<EcamSoftKeyBar, string>(nameof(Button3Text), defaultValue: "BTN3");

    public string Button1Text
    {
        get => GetValue(Button1TextProperty);
        set => SetValue(Button1TextProperty, value);
    }

    public ICommand? Button1Command
    {
        get => GetValue(Button1CommandProperty);
        set => SetValue(Button1CommandProperty, value);
    }

    public string Button2Text
    {
        get => GetValue(Button2TextProperty);
        set => SetValue(Button2TextProperty, value);
    }

    public ICommand? Button2Command
    {
        get => GetValue(Button2CommandProperty);
        set => SetValue(Button2CommandProperty, value);
    }

    public string Button3Text
    {
        get => GetValue(Button3TextProperty);
        set => SetValue(Button3TextProperty, value);
    }

    public EcamSoftKeyBar()
    {
        InitializeComponent();
    }

    public event EventHandler<RoutedEventArgs>? Button3Click;

    private void OnButton3Click(object? sender, RoutedEventArgs e)
    {
        Button3Click?.Invoke(this, e);
    }
}

