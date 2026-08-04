#nullable enable

using System.IO;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// seats-LATEST → SoftOrganBand cabin chrome only.
/// Does not SelectMfdPage / Prefer OneOf — SoftOrgan desk ≠ show-page intent
/// (agent|operator|citizen command / presentation / chord / land).
/// </summary>
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
