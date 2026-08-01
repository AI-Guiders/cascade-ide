#nullable enable

using System.IO;
using System.Windows.Threading;

namespace CDP.GlassCockpit.Windows;

/// <summary>shared-LATEST → EditorPathLabel co-presence chrome (Avalonia CdpSharedFileProjector parity).</summary>
public partial class MainWindow
{
    string? _sharedLatchPath;
    bool _sharedLatchOn;

    void OnSharedChanged(string path)
    {
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var raw = File.ReadAllText(path);
                var view = LatchPaint.PaintShared(raw);
                if (view is null)
                    return;

                _sharedLatchPath = view.Path;
                _sharedLatchOn = view.Shared;
                RefreshEditorSharedChrome();
                StatusText.Text =
                    $"glass · {view.StatusLine} · {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"glass · shared fail · {ex.Message}";
            }
        }, DispatcherPriority.Background);
    }

    void RefreshEditorSharedChrome()
    {
        if (string.IsNullOrWhiteSpace(_editorPath))
            return;

        var basePath = _editorPath;
        var match = _sharedLatchOn
            && !string.IsNullOrWhiteSpace(_sharedLatchPath)
            && PathsReferToSameFile(basePath, _sharedLatchPath);
        EditorPathLabel.Text = match
            ? basePath + LatchPaint.SharedSuffix
            : basePath;
    }

    static bool PathsReferToSameFile(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;
        try
        {
            return string.Equals(
                Path.GetFullPath(a),
                Path.GetFullPath(b),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
