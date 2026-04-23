using Avalonia.Controls;

namespace CascadeIDE.Views;

/// <summary>
/// Набор страниц вторичного контура Mfd (оверлей без TabControl): общий тёмный <see cref="ThemeVariantScope"/> и конвертер <see cref="MfdShellPageEqualsConverter"/>.
/// </summary>
public partial class MfdShellPageStack : UserControl
{
    public MfdShellPageStack() => InitializeComponent();
}
