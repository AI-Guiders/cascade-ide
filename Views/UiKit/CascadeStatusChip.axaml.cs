using Avalonia;
using Avalonia.Controls;

namespace CascadeIDE.Views.UiKit;

/// <summary>Компактный статус (loading, hint) — IDE chrome.</summary>
public partial class CascadeStatusChip : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<CascadeStatusChip, string?>(nameof(Text));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public CascadeStatusChip() => InitializeComponent();
}
