using Avalonia.Controls;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

[SettingsPanel(SettingsPanelIds.MarkdownLsp, "Markdown LSP", "Контент", order: 50)]
public partial class SettingsMarkdownLspPanelView : UserControl
{
    public SettingsMarkdownLspPanelView() => InitializeComponent();
}
