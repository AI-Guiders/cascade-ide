#nullable enable

using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.SoftInstrument;

namespace CDP.GlassCockpit.Windows;

/// <summary>SoftInstrument chrome — GlassStatusChip strip (not multiline prose). Full hint in ToolTip.</summary>
public partial class MainWindow
{
    readonly SoftInstrumentChromeAggregator _softOrgans = new();

    sealed class SoftInstrumentChipVm
    {
        public required string Label { get; init; }
        public required string Tip { get; init; }
        public required GlassChipLevel Level { get; init; }
    }

    void OnSoftInstrumentChanged(string organId, string? chromeHint)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _softOrgans.Apply(organId, chromeHint);
            PaintSoftInstrumentBand();
            UpdateMfdBody();
            if (SoftInstrumentLatchCatalog.Canonicalize(organId)
                .Equals("debug_desk", StringComparison.OrdinalIgnoreCase))
                OnDebugDeskLatchChanged();
        }, DispatcherPriority.Background);
    }

    void SoftInstrumentOverflow_OnClick(object sender, MouseButtonEventArgs e)
    {
        _softOrgans.ToggleExpanded();
        PaintSoftInstrumentBand();
        e.Handled = true;
    }

    void PaintSoftInstrumentBand()
    {
        var band = _softOrgans.SnapshotChips();
        if (!band.HasContent)
        {
            SoftInstrumentChips.ItemsSource = null;
            SoftInstrumentOverflow.Text = "";
            SoftInstrumentOverflowHost.Visibility = Visibility.Collapsed;
            SoftInstrumentBand.Visibility = Visibility.Collapsed;
            return;
        }

        SoftInstrumentChips.ItemsSource = band.Visible.Select(c => new SoftInstrumentChipVm
        {
            Label = c.Label,
            Tip = $"{c.Id}\n{c.ToolTip}",
            Level = c.Level
        }).ToList();

        if (band.IsExpanded)
        {
            SoftInstrumentOverflow.Text = "\u2212";
            SoftInstrumentOverflowHost.ToolTip = SoftInstrumentChromeAggregator.CollapseLabel;
            SoftInstrumentOverflowHost.Visibility = Visibility.Visible;
        }
        else if (band.HiddenCount > 0)
        {
            SoftInstrumentOverflow.Text = $"+{band.HiddenCount}";
            SoftInstrumentOverflowHost.ToolTip = $"+{band.HiddenCount} SoftInstrument · click to expand";
            SoftInstrumentOverflowHost.Visibility = Visibility.Visible;
        }
        else
        {
            SoftInstrumentOverflow.Text = "";
            SoftInstrumentOverflowHost.ToolTip = null;
            SoftInstrumentOverflowHost.Visibility = Visibility.Collapsed;
        }

        SoftInstrumentBand.Visibility = Visibility.Visible;
    }
}
