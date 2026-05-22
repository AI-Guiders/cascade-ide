using Avalonia.Controls;
using CascadeIDE.Models;

namespace CascadeIDE.Views;

[SettingsPanel(SettingsPanelIds.AiChat, "AI и чат", "Агент", order: 10)]
public partial class AiChatSettingsPanelView : UserControl
{
    public AiChatSettingsPanelView() => InitializeComponent();
}
