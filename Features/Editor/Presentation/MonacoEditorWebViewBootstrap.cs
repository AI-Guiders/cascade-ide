using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Platform;
using CascadeIDE.Features.Editor.Application.Monaco;
using Microsoft.Web.WebView2.Core;

namespace CascadeIDE.Features.Editor.Presentation;

/// <summary>Maps vendored cide-editor assets to a https virtual host (WebView2 file:// breaks Monaco AMD).</summary>
internal static class MonacoEditorWebViewBootstrap
{
    public static bool TryMapVirtualHost(NativeWebView webView)
    {
        if (!OperatingSystem.IsWindows())
            return false;

        return TryMapVirtualHostWindows(webView);
    }

    [SupportedOSPlatform("windows")]
    private static bool TryMapVirtualHostWindows(NativeWebView webView)
    {
        if (webView.TryGetPlatformHandle() is not IWindowsWebView2PlatformHandle win)
            return false;

        var core = TryGetCoreWebView2(win);
        if (core is null)
            return false;

        var root = MonacoEditorAssetLocator.GetCideEditorRoot();
        if (!Directory.Exists(root))
            return false;

        core.SetVirtualHostNameToFolderMapping(
            MonacoEditorAssetLocator.VirtualHostName,
            root,
            CoreWebView2HostResourceAccessKind.Allow);
        return true;
    }

    [SupportedOSPlatform("windows")]
    private static CoreWebView2? TryGetCoreWebView2(IWindowsWebView2PlatformHandle handle)
    {
        if (handle.CoreWebView2 == IntPtr.Zero)
            return null;

        try
        {
            return (CoreWebView2)Marshal.GetObjectForIUnknown(handle.CoreWebView2);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco CoreWebView2: " + ex.Message);
            return null;
        }
    }
}
