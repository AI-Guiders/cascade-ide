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

        if (string.IsNullOrWhiteSpace(_editorPath))
        {
            EditorSituCardsPanel.Items.Clear();
            if (EditorSituStatusLabel is not null)
                EditorSituStatusLabel.Text = "situ · empty";
            EditorSituPanel.Visibility = Visibility.Collapsed;
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
            sourceText: CodeEditor?.Text);

        var chips = GlassGlanceCards.BuildEditorSitu(face);
        var factory = new FrameworkElementFactory(typeof(UniformGrid));
        factory.SetValue(UniformGrid.ColumnsProperty, 3);
        EditorSituCardsPanel.ItemsPanel = new ItemsPanelTemplate(factory);
        EditorSituCardsPanel.Items.Clear();
        foreach (var chip in chips)
            EditorSituCardsPanel.Items.Add(CreateDeckCard(chip));

        if (EditorSituStatusLabel is not null)
            EditorSituStatusLabel.Text = face.HasAny
                ? $"situ · card deck · {chips.Count} · {chips[0].Value}"
                : "situ · empty";

        EditorSituPanel.Visibility = face.HasAny
            ? Visibility.Visible
            : Visibility.Collapsed;

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
