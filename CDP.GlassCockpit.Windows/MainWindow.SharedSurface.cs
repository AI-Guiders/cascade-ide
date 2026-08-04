#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Threading;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>shared-LATEST → EditorPathLabel co-presence chrome (Avalonia CdpSharedFileProjector parity).</summary>
public partial class MainWindow
{
    string? _sharedLatchPath;
    bool _sharedLatchOn;
    string? _planWhy;
    string? _planLeaf;

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
        RefreshEditorSituRibbon();
    }

    void RefreshEditorSituRibbon()
    {
        if (EditorSituRibbon is null)
            return;

        if (string.IsNullOrWhiteSpace(_editorPath))
        {
            EditorSituRibbon.Text = string.Empty;
            EditorSituRibbon.Visibility = Visibility.Collapsed;
            return;
        }

        var line = GlassEditorSituRibbon.Format(
            _editorPath,
            _session.WorkspaceRoot,
            _planWhy,
            _planLeaf,
            blastMax: 3);
        EditorSituRibbon.Text = line;
        EditorSituRibbon.Visibility = string.IsNullOrWhiteSpace(line)
            ? Visibility.Collapsed
            : Visibility.Visible;
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
