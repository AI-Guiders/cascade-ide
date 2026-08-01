#nullable enable

using System.IO;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

/// <summary>land-LATEST → AvalonEdit OpenCodeFile + caret line (Avalonia CdpLandProjector parity).</summary>
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

                OpenCodeFile(view.Path, view.Line);
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
