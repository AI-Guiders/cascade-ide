#nullable enable

using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>SoftOrgan chrome band — latch apply, density paint, overflow toggle.</summary>
public partial class MainWindow
{
    readonly SoftOrganChromeAggregator _softOrgans = new();

    void OnSoftOrganChanged(string organId, string? chromeHint)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _softOrgans.Apply(organId, chromeHint);
            PaintSoftOrganBand();
            UpdateMfdBody();
        }, DispatcherPriority.Background);
    }

    void SoftOrganOverflow_OnClick(object sender, MouseButtonEventArgs e)
    {
        _softOrgans.ToggleExpanded();
        PaintSoftOrganBand();
        e.Handled = true;
    }

    void PaintSoftOrganBand()
    {
        var band = _softOrgans.Snapshot();
        if (!band.HasContent)
        {
            SoftOrganHintLines.ItemsSource = null;
            SoftOrganOverflow.Text = "";
            SoftOrganOverflow.Visibility = Visibility.Collapsed;
            SoftOrganBand.Visibility = Visibility.Collapsed;
            return;
        }

        SoftOrganHintLines.ItemsSource = band.VisibleLines;
        SoftOrganOverflow.Text = band.OverflowLine ?? "";
        SoftOrganOverflow.Visibility = band.HasOverflow
            ? Visibility.Visible
            : Visibility.Collapsed;
        SoftOrganBand.Visibility = Visibility.Visible;
    }
}
