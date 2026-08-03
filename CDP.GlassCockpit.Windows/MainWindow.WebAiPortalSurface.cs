#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD WebAiPortal — embedded WebView2 (Avalonia portal EOL for cabin).</summary>
public partial class MainWindow
{
    bool _webAiReady;
    bool _webAiEnsuring;

    void RefreshMfdWebAiVisibility()
    {
        if (MfdWebAiHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "WebAiPortal", StringComparison.OrdinalIgnoreCase);
        MfdWebAiHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show)
            _ = EnsureWebAiAsync(navigateIfBlank: true);
    }

    bool IsWebAiHostActive()
    {
        if (MfdWebAiHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "WebAiPortal", StringComparison.OrdinalIgnoreCase)
               && MfdWebAiHost.Visibility == Visibility.Visible;
    }

    internal void WebAiGo_OnClick(object sender, RoutedEventArgs e) =>
        _ = NavigateWebAiAsync(WebAiUrl?.Text);

    internal void WebAiUrl_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        _ = NavigateWebAiAsync(WebAiUrl?.Text);
    }

    async Task EnsureWebAiAsync(bool navigateIfBlank)
    {
        if (WebAiView is null || _webAiEnsuring)
            return;

        _webAiEnsuring = true;
        try
        {
            if (!_webAiReady)
            {
                if (WebAiStatusLabel is not null)
                    WebAiStatusLabel.Text = "webai · WebView2 · ensuring…";
                await WebAiView.EnsureCoreWebView2Async();
                WebAiView.NavigationCompleted -= WebAiView_OnNavigationCompleted;
                WebAiView.NavigationCompleted += WebAiView_OnNavigationCompleted;
                WebAiView.CoreWebView2.SourceChanged -= WebAiView_OnSourceChanged;
                WebAiView.CoreWebView2.SourceChanged += WebAiView_OnSourceChanged;
                _webAiReady = true;
            }

            if (navigateIfBlank)
            {
                var src = WebAiView.Source?.AbsoluteUri;
                if (string.IsNullOrWhiteSpace(src)
                    || string.Equals(src, "about:blank", StringComparison.OrdinalIgnoreCase))
                    await NavigateWebAiAsync(WebAiUrl?.Text);
            }
        }
        catch (Exception ex)
        {
            _webAiReady = false;
            if (WebAiStatusLabel is not null)
                WebAiStatusLabel.Text = $"webai · WebView2 fail · {ex.Message}";
        }
        finally
        {
            _webAiEnsuring = false;
        }
    }

    async Task NavigateWebAiAsync(string? raw)
    {
        if (WebAiView is null)
            return;

        if (!TryNormalizeWebAiUrl(raw, out var uri, out var text))
        {
            if (WebAiStatusLabel is not null)
                WebAiStatusLabel.Text = "webai · bad URL";
            return;
        }

        if (WebAiUrl is not null)
            WebAiUrl.Text = text;

        await EnsureWebAiAsync(navigateIfBlank: false);
        if (!_webAiReady)
            return;

        try
        {
            WebAiView.Source = uri;
            if (WebAiStatusLabel is not null)
                WebAiStatusLabel.Text = $"webai · nav · {text}";
            StatusText.Text = "glass · webai · WebView2";
        }
        catch (Exception ex)
        {
            if (WebAiStatusLabel is not null)
                WebAiStatusLabel.Text = $"webai · nav fail · {ex.Message}";
        }
    }

    void WebAiView_OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (WebAiStatusLabel is null)
            return;
        WebAiStatusLabel.Text = e.IsSuccess
            ? $"webai · ok · {WebAiView?.Source?.AbsoluteUri}"
            : $"webai · nav err · {e.WebErrorStatus}";
    }

    void WebAiView_OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        var abs = WebAiView?.Source?.AbsoluteUri;
        if (string.IsNullOrWhiteSpace(abs) || WebAiUrl is null)
            return;
        if (!string.Equals(WebAiUrl.Text, abs, StringComparison.Ordinal))
            WebAiUrl.Text = abs;
    }

    /// <summary>Thin peel of Avalonia WebAiPortalUrlNormalize (no Features link).</summary>
    static bool TryNormalizeWebAiUrl(string? raw, out Uri uri, out string text)
    {
        uri = new Uri("about:blank");
        text = "about:blank";

        var s = raw?.Trim() ?? "";
        if (s.Length == 0)
            return true;

        if (s.StartsWith("//", StringComparison.Ordinal))
            s = "https:" + s;

        if (Uri.TryCreate(s, UriKind.Absolute, out var abs)
            && (abs.Scheme is "http" or "https" or "about"))
        {
            uri = abs;
            text = abs.AbsoluteUri;
            return true;
        }

        var preferHttp = s.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
                         || s.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                         || s.StartsWith("[::1]", StringComparison.OrdinalIgnoreCase);
        var prefixes = preferHttp
            ? new[] { "http://", "https://" }
            : new[] { "https://", "http://" };
        foreach (var prefix in prefixes)
        {
            if (Uri.TryCreate(prefix + s.TrimStart('/'), UriKind.Absolute, out abs)
                && abs.Scheme is "http" or "https")
            {
                uri = abs;
                text = abs.AbsoluteUri;
                return true;
            }
        }

        return false;
    }
}
