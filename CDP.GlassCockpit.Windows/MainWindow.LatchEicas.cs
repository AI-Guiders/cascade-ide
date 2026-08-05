#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>Latch presentation/alert/qrh/ecl → Plan + EICAS health band.</summary>
public partial class MainWindow
{
    readonly EicasBandAggregator _eicas = new();

    void OnPresentationChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintPresentation(raw);
                // Topology/MFD only — Plan paints from plan-LATEST (OnPlanChanged).
                var layout = _session.ApplyTopology(view.Topology);
                TopologyBadge.Text = layout.Topology;
                SyncHostWindows();
                // Single-TopLevel OneOf: Sync XOR-paints live cols + patches session.
                // Do not re-apply Resolve default (stack[0]) — that wiped PreferSurface.
                if (!_hosts.IsMainScanOneOf)
                    WpfMainGridColumns.Apply(MainGrid, layout.ColumnDefinitions);

                SelectMfdPage(view.MfdPage, sticky: true);
                RefreshEicasHealth();
                StatusText.Text =
                    $"glass · {view.StatusLine} · cols={_session.Layout.ColumnDefinitions} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · presentation fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void OnPlanChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintPlan(raw);
                PlanTitle.Text = view.Headline;
                PlanMeta.Text = view.Detail;
                PlanWhyReadout.ValueText = view.Why;
                PlanNextReadout.ValueText = view.Next;
                PlanNextReadout.SubText = view.NextSub;
                PlanCourseReadout.ValueText = string.IsNullOrWhiteSpace(view.Course) ? "—" : view.Course;
                PlanCourseReadout.SubText = string.IsNullOrWhiteSpace(view.Wall) ? null : view.Wall;
                PlanCourseReadout.Visibility = string.IsNullOrWhiteSpace(view.Course) && string.IsNullOrWhiteSpace(view.Wall)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                if (PlanLeafBoardList is not null)
                {
                    PlanLeafBoardList.Items.Clear();
                    foreach (var line in view.Board ?? Array.Empty<string>())
                        PlanLeafBoardList.Items.Add(line);
                }
                // Legacy mirror (collapsed).
                PlanReadout.ValueText = view.Headline;
                PlanReadout.SubText = string.IsNullOrWhiteSpace(view.Detail) ? null : view.Detail;
                // Cache leaf/why for Editor situ ribbon (Shared-SSOT Q2).
                _planLeaf = view.Headline;
                _planWhy = string.IsNullOrWhiteSpace(view.Why) || view.Why == "—" ? null : view.Why;
                RefreshEditorSituRibbon();
                // Plan paint ≠ OneOf Prefer P — see PresentationPmOneOfPolicy.FromPlanLatch.
                StatusText.Text = $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · plan fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void OnAlertChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintAlert(raw);
                _eicas.Apply("alert", view?.StatusLine);
                RefreshEicasHealth();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · alert fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void OnQrhChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintQrh(raw);
                _eicas.Apply("qrh", view?.StatusLine);
                RefreshEicasHealth();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · qrh fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void OnEclChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintEcl(raw);
                _eicas.Apply("ecl", view?.StatusLine);
                RefreshEicasHealth();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · ecl fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void RefreshEicasHealth()
    {
        if (MfdHealthBand is null)
            return;

        MfdHealthBand.Items.Clear();

        var chips = _eicas.BandChips;
        if (chips.Count == 0)
        {
            _eicasClrSuppressPulse = null;
            MfdHealthBand.Items.Add(MakeEicasChip(
                $"EICAS · CLEAR · {CurrentMfdPage()}",
                "idle"));
            return;
        }

        // CLR SoftKey: hide band until latch pulse changes (master-caution cancel feel).
        if (_eicasClrSuppressPulse is not null)
        {
            var pulse = ReadEclPulse() ?? ReadAlertPulse() ?? "";
            if (string.Equals(pulse, _eicasClrSuppressPulse, StringComparison.Ordinal))
            {
                MfdHealthBand.Items.Add(MakeEicasChip(
                    $"EICAS · CLR · {CurrentMfdPage()}",
                    "idle"));
                return;
            }

            _eicasClrSuppressPulse = null;
        }

        for (var i = 0; i < chips.Count; i++)
        {
            var chip = chips[i];
            var block = MakeEicasChip(chip.Text, chip.Severity);
            if (i > 0)
                block.Margin = new Thickness(12, 0, 0, 0);
            MfdHealthBand.Items.Add(block);
        }
    }

    TextBlock MakeEicasChip(string text, string severity)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = severity switch
            {
                "warn" => Brushes.OrangeRed,
                "caut" => Brushes.Gold,
                "adv" => Brushes.DeepSkyBlue,
                _ => MutedFg()
            }
        };
    }

    Brush MutedFg()
    {
        try
        {
            return (Brush)FindResource("Glass.FgMuted");
        }
        catch
        {
            return Brushes.Gray;
        }
    }
}
