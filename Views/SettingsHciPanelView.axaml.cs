using Avalonia.Controls;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

[SettingsPanel(SettingsPanelIds.Hci, "Hybrid Codebase Index", "Индекс", order: 60)]
public partial class SettingsHciPanelView : UserControl
{
    public SettingsHciPanelView() => InitializeComponent();
}
