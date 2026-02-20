using Avalonia.Controls;

namespace CascadeIDE.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        var btn = this.FindControl<Button>("CloseButton");
        if (btn is not null)
            btn.Click += (_, _) => Close();
    }
}
