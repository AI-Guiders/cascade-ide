#nullable enable

using System.IO;
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
                var headline = string.IsNullOrWhiteSpace(view.Headline) ? "Presentation" : view.Headline;
                PlanTitle.Text = headline;
                PlanMeta.Text = view.Detail;
                PlanReadout.ValueText = headline;
                PlanReadout.SubText = string.IsNullOrWhiteSpace(view.Detail) ? null : view.Detail;

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
        if (MfdHealth is null)
            return;

        var text = _eicas.BandText;
        if (!string.IsNullOrWhiteSpace(text))
        {
            MfdHealth.Text = text;
            MfdHealth.Foreground = _eicas.Severity switch
            {
                "warn" => Brushes.OrangeRed,
                "caut" => Brushes.Gold,
                "adv" => Brushes.DeepSkyBlue,
                _ => MutedFg()
            };
            return;
        }

        MfdHealth.Text = $"EICAS · CLEAR · {CurrentMfdPage()}";
        MfdHealth.Foreground = MutedFg();
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
