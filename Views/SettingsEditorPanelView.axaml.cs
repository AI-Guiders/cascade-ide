using Avalonia.Controls;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

[SettingsPanel(SettingsPanelIds.Editor, "Редактор", "Редактор", order: 20)]
public partial class SettingsEditorPanelView : UserControl
{
    public SettingsEditorPanelView() => InitializeComponent();
}
