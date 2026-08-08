#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Threading;
using CascadeIDE.GlassCore.Presentation;
using CascadeIDE.Services.Presentation;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// seats-LATEST → SoftOrganBand cabin chrome.
/// Quiet republish: chrome tip only (no MFD steal).
/// <c>show_face</c> (PlaceOrgan / Citizen go): BringCabinAttention + SelectMfd or Prefer P.
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

                if (view.ShowFace)
                {
                    BringCabinAttention();
                    if (PresentationPmOneOfPolicy.SeatsMaySelectMfd(view.ShowFace, view.MfdPage))
                        SelectMfdPage(view.MfdPage, sticky: true);
                    else if (string.Equals(view.FaceSeat, "p", StringComparison.OrdinalIgnoreCase))
                        _hosts.PreferPmOneOf(PresentationAnchorKind.Pfd);

                    // Sticky web_ai_url must not steal Face on every PlaceOrgan — only browser Face.
                    if (view.WantsWebAiNavigate)
                        RunWebAiPortal(view.WebAiUrl);
                }

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

    /// <summary>Unminimize + Activate Glass so human sees PlaceOrgan Face without Cursor roundtrip.</summary>
    void BringCabinAttention()
    {
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        if (!IsVisible)
            Show();

        Activate();
        // Brief Topmost pulse — Activate alone often loses to Cursor foreground.
        Topmost = true;
        Topmost = false;
    }
}
