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
                WpfMainGridColumns.Apply(MainGrid, layout.ColumnDefinitions);
                TopologyBadge.Text = layout.Topology;
                SyncHostWindows();

                SelectMfdPage(view.MfdPage);
                RefreshEicasHealth();
                StatusText.Text =
                    $"glass · {view.StatusLine} · cols={layout.ColumnDefinitions} · {DateTime.Now:HH:mm:ss}";
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
                PlanReadout.ValueText = view.Headline;
                PlanReadout.SubText = string.IsNullOrWhiteSpace(view.Detail) ? null : view.Detail;
                // Cache leaf/why for Editor situ ribbon (Shared-SSOT Q2).
                _planLeaf = view.Headline;
                _planWhy = ExtractPlanWhy(view.Detail);
                RefreshEditorSituRibbon();
                _hosts.PreferPmOneOf(CascadeIDE.GlassCore.Presentation.PresentationPmOneOfPolicy.FromPlanLatch());
                StatusText.Text = $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · plan fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    static string? ExtractPlanWhy(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return null;
        const string prefix = "WHY · ";
        var line = detail.Replace("\r\n", "\n").Split('\n')[0].Trim();
        if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return line[prefix.Length..].Trim();
        return null;
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
            MfdHealthBand.Items.Add(MakeEicasChip(
                $"EICAS · CLEAR · {CurrentMfdPage()}",
                "idle"));
            return;
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
