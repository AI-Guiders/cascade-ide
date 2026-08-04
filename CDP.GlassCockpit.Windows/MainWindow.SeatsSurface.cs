#nullable enable

using System.IO;
using System.Windows.Threading;
using CascadeIDE.GlassCore.Presentation;

namespace CDP.GlassCockpit.Windows;

/// <summary>seats-LATEST → SelectMfdPage + SoftOrganBand cabin chrome (Avalonia CdpSeatsProjector parity).</summary>
public partial class MainWindow
{
    string? _lastSeatsMOrgan;

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

                var mOrgan = view.MOrgan;
                // First seats hydrate after sticky presentation must not count as M change
                // (null→browser would yank Editor). Only real SoftOrgan M pin flips win.
                var mChanged = _lastSeatsMOrgan is not null
                    && !string.Equals(
                        _lastSeatsMOrgan,
                        mOrgan,
                        StringComparison.OrdinalIgnoreCase);
                _lastSeatsMOrgan = mOrgan;

                if (PresentationPmOneOfPolicy.SeatsMaySelectMfd(
                        _stickyMfdPage,
                        view.MfdPage,
                        mChanged))
                {
                    // SoftOrgan M pin change becomes the new sticky instrument.
                    SelectMfdPage(view.MfdPage, sticky: mChanged);
                }

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
