#nullable enable

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Threading;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>disk-LATEST → AvalonEdit reload / human Save publish (Avalonia CdpDiskSyncProjector parity).</summary>
public partial class MainWindow
{
    static readonly JsonSerializerOptions DiskPublishOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    DateTimeOffset _diskSuppressPublishUntil = DateTimeOffset.MinValue;

    void OnDiskChanged(string latchPath)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(latchPath);
                var view = LatchPaint.PaintDisk(raw);
                if (view is null)
                    return;

                // Agent Instant Save → reload open editor. Human origin is peer publish (Avalonia watches agent).
                if (!string.Equals(view.Origin, LatchPaint.DiskOriginAgent, StringComparison.OrdinalIgnoreCase))
                    return;

                if (!File.Exists(view.Path))
                {
                    StatusText.Text = $"glass · disk miss · {view.Path}";
                    return;
                }

                if (string.IsNullOrWhiteSpace(_editorPath)
                    || !PathsReferToSameFile(_editorPath, view.Path))
                {
                    StatusText.Text =
                        $"glass · {view.StatusLine} · skip (not open) · {DateTime.Now:HH:mm:ss}";
                    return;
                }

                var caretLine = CodeEditor.TextArea.Caret.Line;
                _diskSuppressPublishUntil = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(400);
                // Quiet reload — do not steal PreferSurface / SelectMfd while Human holds Portal.
                OpenCodeFile(view.Path, caretLine > 0 ? caretLine : null, showFace: false);
                // MFD host may not host StatusText — mark path chrome for dual-cockpit dogfood.
                EditorPathLabel.Text = EditorPathLabel.Text + " · disk";
                StatusText.Text =
                    $"glass · {view.StatusLine} · reloaded · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · disk fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    /// <summary>Human Save → agent buffer reload peer (writes disk-LATEST origin=human).</summary>
    void PublishHumanDiskSave(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        if (DateTimeOffset.UtcNow < _diskSuppressPublishUntil)
            return;

        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var body = new
            {
                schema = LatchPaint.DiskSchema,
                path = Path.GetFullPath(path),
                origin = LatchPaint.DiskOriginHuman,
                stamped_utc = DateTimeOffset.UtcNow
            };
            var json = JsonSerializer.Serialize(body, DiskPublishOpts);
            var latch = CdpHabitatPaths.DiskLatchPath;
            var tmp = latch + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, latch, overwrite: true);
        }
        catch
        {
            /* best-effort */
        }
    }
}
