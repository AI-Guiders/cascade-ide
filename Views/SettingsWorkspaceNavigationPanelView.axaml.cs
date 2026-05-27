using Avalonia.Controls;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

[SettingsPanel(SettingsPanelIds.WorkspaceNavigationMap, "Карта намерений (PFD)", "Рабочая область", order: 25)]
public partial class SettingsWorkspaceNavigationPanelView : UserControl
{
    public SettingsWorkspaceNavigationPanelView() => InitializeComponent();
}

