#nullable enable

using System.Windows;
using System.Windows.Controls;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD AiChatSettings — read-only settings.toml host (provider/model/MCP mounts).</summary>
public partial class MainWindow
{
    void RefreshMfdAiChatSettingsVisibility()
    {
        if (MfdAiChatSettingsHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "AiChatSettings", StringComparison.OrdinalIgnoreCase);
        MfdAiChatSettingsHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show)
            RefreshAiChatSettingsHost();
    }

    bool IsAiChatSettingsHostActive()
    {
        if (MfdAiChatSettingsHost is null)
            return false;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "AiChatSettings", StringComparison.OrdinalIgnoreCase)
               && MfdAiChatSettingsHost.Visibility == Visibility.Visible;
    }

    internal void AiChatSettingsRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshAiChatSettingsHost();

    void RefreshAiChatSettingsHost()
    {
        var snap = GlassAiChatSettingsGlance.TryLoad(_session.WorkspaceRoot);
        if (AiChatSettingsStatusLabel is not null)
            AiChatSettingsStatusLabel.Text = GlassAiChatSettingsGlance.FormatHeader(snap);
        if (AiChatSettingsToml is not null)
            AiChatSettingsToml.Text = GlassAiChatSettingsGlance.FormatBody(snap);
    }
}
