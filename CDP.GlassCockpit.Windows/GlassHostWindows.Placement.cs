#nullable enable
using System.Windows;
using CascadeIDE.Services;

namespace CDP.GlassCockpit.Windows;

/// <summary>Satellite TopLevel place/restore/persist (settings.toml ADR 0017).</summary>
internal sealed partial class GlassHostWindows
{
    enum SatelliteRole { Pfd, Mfd, Pm }

    /// <summary>
    /// SoftFL densify: restore satellite TopLevel from settings.toml (ADR 0017 fields),
    /// else heuristic; persist on move/resize like Avalonia host windows.
    /// </summary>
    void PlaceSatellite(Window host, int screenHint, SatelliteRole role)
    {
        WireSatellitePersist(host, role);
        if (TryRestoreSatellite(host, role))
            return;

        host.Width = Math.Max(640, _main.Width * 0.7);
        host.Height = Math.Max(480, _main.Height * 0.85);

        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var primaryW = SystemParameters.PrimaryScreenWidth;
        var virtualW = SystemParameters.VirtualScreenWidth;

        if (screenHint > 0 && virtualW > primaryW + 80)
        {
            host.Left = virtualLeft + primaryW + 24;
            host.Top = virtualTop + 48;
        }
        else
        {
            host.Left = _main.Left + 36 + screenHint * 28;
            host.Top = _main.Top + 36 + screenHint * 28;
        }
    }

    bool TryRestoreSatellite(Window host, SatelliteRole role)
    {
        var s = _main.Session.Settings;
        int? x = null, y = null;
        double? w = null, h = null;
        switch (role)
        {
            case SatelliteRole.Pfd:
                x = s.PfdHostWindowPixelX; y = s.PfdHostWindowPixelY;
                w = s.PfdHostWindowWidth; h = s.PfdHostWindowHeight;
                break;
            case SatelliteRole.Mfd:
                x = s.MfdHostWindowPixelX; y = s.MfdHostWindowPixelY;
                w = s.MfdHostWindowWidth; h = s.MfdHostWindowHeight;
                break;
            case SatelliteRole.Pm:
                x = s.PmSplitHostWindowPixelX; y = s.PmSplitHostWindowPixelY;
                w = s.PmSplitHostWindowWidth; h = s.PmSplitHostWindowHeight;
                break;
        }

        if (x is null || y is null || w is null || h is null)
            return false;

        var left = x.Value;
        var top = y.Value;
        var width = Math.Max(320, w.Value);
        var height = Math.Max(240, h.Value);

        var vLeft = SystemParameters.VirtualScreenLeft;
        var vTop = SystemParameters.VirtualScreenTop;
        var vRight = vLeft + SystemParameters.VirtualScreenWidth;
        var vBottom = vTop + SystemParameters.VirtualScreenHeight;
        // Require title-bar center inside virtual desktop (monitor still present).
        var cx = left + width / 2;
        var cy = top + 20;
        if (cx < vLeft || cx > vRight || cy < vTop || cy > vBottom)
            return false;

        host.Left = left;
        host.Top = top;
        host.Width = width;
        host.Height = height;
        return true;
    }

    void WireSatellitePersist(Window host, SatelliteRole role)
    {
        void Persist()
        {
            if (!host.IsVisible || host.WindowState == WindowState.Minimized)
                return;
            var left = (int)Math.Round(host.Left);
            var top = (int)Math.Round(host.Top);
            var width = host.Width;
            var height = host.Height;
            var s = _main.Session.Settings;
            switch (role)
            {
                case SatelliteRole.Pfd:
                    s.PfdHostWindowPixelX = left; s.PfdHostWindowPixelY = top;
                    s.PfdHostWindowWidth = width; s.PfdHostWindowHeight = height;
                    break;
                case SatelliteRole.Mfd:
                    s.MfdHostWindowPixelX = left; s.MfdHostWindowPixelY = top;
                    s.MfdHostWindowWidth = width; s.MfdHostWindowHeight = height;
                    break;
                case SatelliteRole.Pm:
                    s.PmSplitHostWindowPixelX = left; s.PmSplitHostWindowPixelY = top;
                    s.PmSplitHostWindowWidth = width; s.PmSplitHostWindowHeight = height;
                    break;
            }

            try { SettingsService.Save(s); }
            catch { /* settings write best-effort */ }
        }

        host.LocationChanged += (_, _) => Persist();
        host.SizeChanged += (_, _) => Persist();
        host.Closed += (_, _) => Persist();
    }
}
