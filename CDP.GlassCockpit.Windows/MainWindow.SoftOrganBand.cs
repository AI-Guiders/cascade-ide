#nullable enable

using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>SoftOrgan chrome — GlassStatusChip strip (not multiline prose). Full hint in ToolTip.</summary>
public partial class MainWindow
{
    readonly SoftOrganChromeAggregator _softOrgans = new();

    sealed class SoftOrganChipVm
    {
        public required string Label { get; init; }
        public required string Tip { get; init; }
        public required GlassChipLevel Level { get; init; }
    }

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
        var band = _softOrgans.SnapshotChips();
        if (!band.HasContent)
        {
            SoftOrganChips.ItemsSource = null;
            SoftOrganOverflow.Text = "";
            SoftOrganOverflowHost.Visibility = Visibility.Collapsed;
            SoftOrganBand.Visibility = Visibility.Collapsed;
            return;
        }

        SoftOrganChips.ItemsSource = band.Visible.Select(c => new SoftOrganChipVm
        {
            Label = c.Label,
            Tip = $"{c.Id}\n{c.ToolTip}",
            Level = c.Level
        }).ToList();

        if (band.IsExpanded)
        {
            SoftOrganOverflow.Text = "\u2212";
            SoftOrganOverflowHost.ToolTip = SoftOrganChromeAggregator.CollapseLabel;
            SoftOrganOverflowHost.Visibility = Visibility.Visible;
        }
        else if (band.HiddenCount > 0)
        {
            SoftOrganOverflow.Text = $"+{band.HiddenCount}";
            SoftOrganOverflowHost.ToolTip = $"+{band.HiddenCount} SoftOrgan · click to expand";
            SoftOrganOverflowHost.Visibility = Visibility.Visible;
        }
        else
        {
            SoftOrganOverflow.Text = "";
            SoftOrganOverflowHost.ToolTip = null;
            SoftOrganOverflowHost.Visibility = Visibility.Collapsed;
        }

        SoftOrganBand.Visibility = Visibility.Visible;
    }
}
