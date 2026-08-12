#nullable enable

using System.IO;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

/// <summary>ignite-wake-LATEST → StatusText + SoftInstrument ignite tip (Composer-free Autoi sight).</summary>
public partial class MainWindow
{
    string? _lastIgniteWakeArmId;

    void OnIgniteWakeChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintIgniteWake(raw);
                if (view is null)
                    return;

                // FileSystemWatcher often fires twice on atomic replace.
                if (string.Equals(_lastIgniteWakeArmId, view.ArmId, StringComparison.OrdinalIgnoreCase))
                {
                    StatusText.Text = $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
                    return;
                }

                _lastIgniteWakeArmId = view.ArmId;
                _softOrgans.Apply("ignite", view.ChromeHint);
                PaintSoftInstrumentBand();
                UpdateMfdBody();
                StatusText.Text =
                    $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · wake fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }
}
