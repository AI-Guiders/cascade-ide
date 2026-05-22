using Avalonia.Controls;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

[SettingsPanel(SettingsPanelIds.Markdown, "Markdown · диаграммы", "Контент", order: 40)]
public partial class SettingsMarkdownPanelView : UserControl
{
    public SettingsMarkdownPanelView() => InitializeComponent();
}
