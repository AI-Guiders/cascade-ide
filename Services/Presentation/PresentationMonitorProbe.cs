using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace CascadeIDE.Services.Presentation;

/// <summary>Reads Avalonia screens for <see cref="PresentationTierResolver"/>.</summary>
public static class PresentationMonitorProbe
{
    public static PresentationMonitorSnapshot Capture()
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
                return PresentationMonitorSnapshot.SingleFallback;

            var main = lifetime.MainWindow;
            var all = main?.Screens?.All;
            if (all is null || all.Count == 0)
                return PresentationMonitorSnapshot.SingleFallback;

            var ordered = PresentationMonitorTopology.OrderScreensForPresentation(all);
            var primary = ordered[0].WorkingArea;
            var totalW = 0;
            for (var i = 0; i < ordered.Count; i++)
                totalW += ordered[i].WorkingArea.Width;

            return new PresentationMonitorSnapshot(
                ordered.Count,
                primary.Width,
                primary.Height,
                totalW);
        }
        catch
        {
            return PresentationMonitorSnapshot.SingleFallback;
        }
    }
}
