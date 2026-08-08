#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        if (EditorSituPanel is null || EditorSituCardsPanel is null)
            return;

        // Operator: situ card deck on Editor = tank HUD (code and prose). Keep path bar;
        // WHY/BLAST/ROLE live on Plan/FDS — not over AvalonEdit. Diff/applies tint stays in gutter.
        EditorSituCardsPanel.Items.Clear();
        if (EditorSituStatusLabel is not null)
            EditorSituStatusLabel.Text = "situ · off · editor";
        EditorSituPanel.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(_editorPath))
        {
            _diffHunkRenderer?.Apply(null);
            _appliesTintRenderer?.Apply(null);
            CodeEditor?.TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Background);
            return;
        }

        var face = GlassEditorSituRibbon.Build(
            _editorPath,
            _session.WorkspaceRoot,
            _planWhy,
            _planLeaf,
            blastMax: 3,
            sourceText: CodeEditor?.Text,
            buildProblems: MergeAppliesProblemSources(),
            testFails: _testFails);

        EnsureDiffHunkRenderer();
        EnsureAppliesTintRenderer();
        _diffHunkRenderer!.Apply(face.Diff);
        _appliesTintRenderer!.Apply(face.Applies);
        CodeEditor.TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Background);
    }

    void EnsureDiffHunkRenderer()
    {
        if (_diffHunkRenderer is not null)
            return;
        _diffHunkRenderer = new GlassEditorDiffHunkRenderer();
        CodeEditor.TextArea.TextView.BackgroundRenderers.Add(_diffHunkRenderer);
    }

    void EnsureAppliesTintRenderer()
    {
        if (_appliesTintRenderer is not null)
            return;
        _appliesTintRenderer = new GlassEditorAppliesTintRenderer();
        CodeEditor.TextArea.TextView.BackgroundRenderers.Add(_appliesTintRenderer);
    }

    /// <summary>Build + Problems lists scoped later by Applies Collect — prefer union without dup keys.</summary>
    IReadOnlyList<GlassProblemItem> MergeAppliesProblemSources()
    {
        if (_buildProblems.Count == 0 && _problemAll.Count == 0)
            return [];
        if (_problemAll.Count == 0)
            return _buildProblems.ToList();
        if (_buildProblems.Count == 0)
            return _problemAll.ToList();
        return GlassRoslynDiagnosticsFeed.MergeDistinct(_buildProblems.ToList(), _problemAll);
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
