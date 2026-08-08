#nullable enable

using System.IO;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// land-LATEST → optional AvalonEdit OpenCodeFile (Avalonia CdpLandProjector parity).
/// Quiet default: status tip only — do not steal PreferSurface / Human Portal stick.
/// </summary>
public partial class MainWindow
{
    void OnLandChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintLand(raw);
                if (view is null)
                    return;
                if (!File.Exists(view.Path))
                {
                    StatusText.Text = $"glass · land miss · {view.Path}";
                    return;
                }

                if (!view.ShowFace)
                {
                    StatusText.Text =
                        $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
                    return;
                }

                OpenCodeFile(view.Path, view.Line, showFace: true);
                StatusText.Text =
                    $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · land fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }
}
