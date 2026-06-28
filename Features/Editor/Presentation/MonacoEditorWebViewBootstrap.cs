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
    private const int VkP = 0x50;
    private const int VkControl = 0x11;

    public static bool TryMapVirtualHost(NativeWebView webView) =>
        TryConfigure(webView, onHostShortcut: null);

    public static bool TryConfigure(NativeWebView webView, Action<string>? onHostShortcut)
    {
        if (!OperatingSystem.IsWindows())
            return false;

        return TryConfigureWindows(webView, onHostShortcut);
    }

    [SupportedOSPlatform("windows")]
    private static bool TryConfigureWindows(NativeWebView webView, Action<string>? onHostShortcut)
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

        if (onHostShortcut is not null
            && TryGetCoreWebView2Controller(win) is { } controller)
        {
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            controller.AcceleratorKeyPressed += (_, e) =>
            {
                if (!IsCtrlPKeyDown(e))
                    return;

                e.Handled = true;
                onHostShortcut("workspace_go_to_file");
            };
        }

        return true;
    }

    [SupportedOSPlatform("windows")]
    private static bool IsCtrlPKeyDown(CoreWebView2AcceleratorKeyPressedEventArgs e)
    {
        if (e.VirtualKey != VkP)
            return false;

        if (e.PhysicalKeyStatus.IsKeyReleased != 0)
            return false;

        var lParam = e.KeyEventLParam;
        if ((lParam & (1 << 31)) != 0)
            return false;

        if ((lParam & (1 << 30)) != 0)
            return false;

        return (GetKeyState(VkControl) & 0x8000) != 0;
    }

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

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

    [SupportedOSPlatform("windows")]
    private static CoreWebView2Controller? TryGetCoreWebView2Controller(IWindowsWebView2PlatformHandle handle)
    {
        if (handle.CoreWebView2Controller == IntPtr.Zero)
            return null;

        try
        {
            return (CoreWebView2Controller)Marshal.GetObjectForIUnknown(handle.CoreWebView2Controller);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Monaco CoreWebView2Controller: " + ex.Message);
            return null;
        }
    }
}
