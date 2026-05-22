using Avalonia.Controls;
using CascadeIDE.Models;

namespace CascadeIDE.Views.Settings;

internal static class SettingsPanelFactory
{
    public static Control? TryCreate(string panelId) => panelId switch
    {
        SettingsPanelIds.Themes => new SettingsThemesPanelView(),
        SettingsPanelIds.AiChat => new AiChatSettingsPanelView(),
        SettingsPanelIds.Editor => new SettingsEditorPanelView(),
        SettingsPanelIds.CSharpLsp => new SettingsCSharpLspPanelView(),
        SettingsPanelIds.Markdown => new SettingsMarkdownPanelView(),
        SettingsPanelIds.MarkdownLsp => new SettingsMarkdownLspPanelView(),
        SettingsPanelIds.Hci => new SettingsHciPanelView(),
        _ => null,
    };
}
