#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>SoftOrgan chrome — indicator chips (not multiline prose). Full hint in ToolTip.</summary>
public partial class MainWindow
{
    readonly SoftOrganChromeAggregator _softOrgans = new();

    sealed class SoftOrganChipVm
    {
        public required string Label { get; init; }
        public required string Tip { get; init; }
        public required Brush Bg { get; init; }
        public required Brush Fg { get; init; }
        public required Brush Border { get; init; }
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

        var quietBg = (Brush)FindResource("Glass.BgRaised");
        var quietFg = (Brush)FindResource("Glass.FgMuted");
        var quietBorder = (Brush)FindResource("Glass.Border");
        var hotBg = (Brush)FindResource("Glass.CautionBg");
        var hotFg = (Brush)FindResource("Glass.CautionFg");
        var hotBorder = (Brush)FindResource("Glass.CautionBorder");

        var vms = band.Visible.Select(c => new SoftOrganChipVm
        {
            Label = c.Label,
            Tip = $"{c.Id}\n{c.ToolTip}",
            Bg = c.Hot ? hotBg : quietBg,
            Fg = c.Hot ? hotFg : quietFg,
            Border = c.Hot ? hotBorder : quietBorder
        }).ToList();

        SoftOrganChips.ItemsSource = vms;

        if (band.IsExpanded)
        {
            SoftOrganOverflow.Text = "−";
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
