#nullable enable

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD WebAiPortal — URL chrome v1 (WebView2 deferred).</summary>
public partial class MainWindow
{
    void RefreshMfdWebAiVisibility()
    {
        if (MfdWebAiHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "WebAiPortal", StringComparison.OrdinalIgnoreCase);
        MfdWebAiHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    bool IsWebAiHostActive()
    {
        if (MfdWebAiHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "WebAiPortal", StringComparison.OrdinalIgnoreCase)
               && MfdWebAiHost.Visibility == Visibility.Visible;
    }

    internal void WebAiGo_OnClick(object sender, RoutedEventArgs e)
    {
        var url = WebAiUrl?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            if (WebAiStatusLabel is not null)
                WebAiStatusLabel.Text = "webai · empty URL";
            return;
        }

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            if (WebAiStatusLabel is not null)
                WebAiStatusLabel.Text = $"webai · opened external · {url}";
            StatusText.Text = "glass · webai · external browser (WebView2 later)";
        }
        catch (Exception ex)
        {
            if (WebAiStatusLabel is not null)
                WebAiStatusLabel.Text = $"webai · fail · {ex.Message}";
        }
    }
}
