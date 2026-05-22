using Avalonia.Controls;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

[SettingsPanel(SettingsPanelIds.CSharpLsp, "C# / LSP", "Редактор", order: 30)]
public partial class SettingsCSharpLspPanelView : UserControl
{
    public SettingsCSharpLspPanelView() => InitializeComponent();
}
