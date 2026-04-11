using Avalonia.Controls;

namespace CascadeIDE.Views;

/// <summary>Второй <c>TopLevel</c> под зону Mfd: тот же <see cref="SecondaryShellView"/>, что и колонка в <see cref="MainWindow"/> (ADR 0017).</summary>
public partial class MfdHostWindow : Window
{
    public MfdHostWindow()
    {
        InitializeComponent();
    }
}
