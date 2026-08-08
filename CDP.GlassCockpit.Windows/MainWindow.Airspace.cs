#nullable enable

using System.Windows;
using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// WebView2 / EasyTerminalControl are native HWNDs — they paint above in-tree WPF overlays.
/// Park them while palette/chord/open-family is open (Popup rejected: floats above other apps).
/// </summary>
public partial class MainWindow
{
    Visibility _webAiParkedVisibility = Visibility.Visible;
    Visibility _terminalVtParkedVisibility = Visibility.Visible;
    bool _airspaceParked;

    bool AnyFloatingOverlayOpen =>
        PaletteOverlay?.Visibility == Visibility.Visible
        || ChordOverlay?.Visibility == Visibility.Visible
        || OpenFamilyOverlay?.Visibility == Visibility.Visible;

    void SetFloatingOverlay(Border? overlay, bool open)
    {
        if (overlay is null)
            return;
        overlay.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        SyncAirspaceHosts();
    }

    void SyncAirspaceHosts()
    {
        var needPark = AnyFloatingOverlayOpen;
        if (needPark == _airspaceParked)
            return;

        if (needPark)
        {
            if (WebAiView is not null)
            {
                _webAiParkedVisibility = WebAiView.Visibility;
                WebAiView.Visibility = Visibility.Collapsed;
            }

            if (TerminalVt is not null)
            {
                _terminalVtParkedVisibility = TerminalVt.Visibility;
                TerminalVt.Visibility = Visibility.Collapsed;
            }

            _airspaceParked = true;
        }
        else
        {
            if (WebAiView is not null)
                WebAiView.Visibility = _webAiParkedVisibility;
            if (TerminalVt is not null)
                TerminalVt.Visibility = _terminalVtParkedVisibility;
            _airspaceParked = false;
        }
    }
}
