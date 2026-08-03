#nullable enable

using System.Windows;
using System.Windows.Controls;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD HybridIndex — live status JSON host + FS glance.</summary>
public partial class MainWindow
{
    void RefreshMfdHybridIndexVisibility()
    {
        if (MfdHybridIndexHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "HybridIndex", StringComparison.OrdinalIgnoreCase);
        MfdHybridIndexHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && string.IsNullOrWhiteSpace(HybridIndexOutput?.Text))
            RefreshHybridIndexBody();
    }

    bool IsHybridIndexHostActive()
    {
        if (MfdHybridIndexHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "HybridIndex", StringComparison.OrdinalIgnoreCase)
               && MfdHybridIndexHost.Visibility == Visibility.Visible;
    }

    internal void HybridIndexRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshHybridIndexBody();

    void RefreshHybridIndexBody()
    {
        if (HybridIndexOutput is null)
            return;

        var body = GlassHybridIndexGlance.TryFormatLiveFromWorkspaceRoot(_session.WorkspaceRoot)
                   ?? "HybridIndex · workspace root unavailable";
        HybridIndexOutput.Text = body;
        if (HybridIndexStatusLabel is not null)
            HybridIndexStatusLabel.Text = body.Contains("status json", StringComparison.Ordinal)
                ? "hci · live status json"
                : "hci · fs glance";
    }
}
