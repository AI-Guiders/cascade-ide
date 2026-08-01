#nullable enable

using System.IO;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

/// <summary>seats-LATEST → SelectMfdPage + SoftOrganBand cabin chrome (Avalonia CdpSeatsProjector parity).</summary>
public partial class MainWindow
{
    void OnSeatsChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintSeats(raw);
                if (view is null)
                    return;

                if (!string.IsNullOrWhiteSpace(view.MfdPage))
                    SelectMfdPage(view.MfdPage);

                _softOrgans.Apply("cabin", view.ChromeHint);
                PaintSoftOrganBand();
                UpdateMfdBody();
                StatusText.Text =
                    $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · seats fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }
}
