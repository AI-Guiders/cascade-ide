#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>AvalonEdit surface — mount, dogfood open, pick/save, Ctrl+O/S.</summary>
public partial class MainWindow
{
    string? _editorPath;
    GlassAvalonEditChrome? _editorChrome;
    GlassAvalonEditTextMate? _editorTextMate;
    GlassEditorDiffHunkRenderer? _diffHunkRenderer;
    GlassEditorAppliesTintRenderer? _appliesTintRenderer;

    void EnsureEditorChrome()
    {
        if (_editorChrome is not null)
            return;
        _editorChrome = new GlassAvalonEditChrome(CodeEditor);
        _editorTextMate ??= new GlassAvalonEditTextMate(CodeEditor);
    }

    void DisposeEditorChrome()
    {
        _editorChrome?.Dispose();
        _editorChrome = null;
        _editorTextMate?.Dispose();
        _editorTextMate = null;
    }

    void MountEditor(ContentControl host, bool refreshVisibility = true)
    {
        if (ReferenceEquals(EditorChrome.Parent, host))
            return;

        switch (EditorChrome.Parent)
        {
            case ContentControl cc:
                cc.Content = null;
                break;
            case Panel panel:
                panel.Children.Remove(EditorChrome);
                break;
        }

        host.Content = EditorChrome;
        if (refreshVisibility)
            RefreshMfdEditorVisibility();
    }

    void RefreshMfdEditorVisibility()
    {
        if (MfdEditorHost is null || MfdBody is null)
            return;

        var page = CurrentMfdPage();
        // Editor Face: MFD page=Editor always shows AvalonEdit on M — never FormatMfdStub peel.
        if (GlassEditorFace.PreferEditorHost(page))
        {
            if (!ReferenceEquals(EditorChrome.Parent, MfdEditorHost))
                MountEditor(MfdEditorHost, refreshVisibility: false);
        }
        else if (ReferenceEquals(EditorChrome.Parent, MfdEditorHost)
                 && !GlassEditorFace.PreferParkOnMfdWhenReleased(_session.IsIntercomForward)
                 && ForwardEditorHost is not null)
        {
            // Face released + Forward owns primary → restore AvalonEdit to Forward (ADR 0120).
            MountEditor(ForwardEditorHost, refreshVisibility: false);
        }

        var editorOnM = ReferenceEquals(EditorChrome.Parent, MfdEditorHost);
        var showEditor = editorOnM && GlassEditorFace.PreferEditorHost(page);
        // SE Face = TreeView always via GlassSolutionExplorerFace.PreferTreeHost — never Avalonia FormatMfdStub peel.
        var showSe = MfdSolutionExplorerTree is not null
            && GlassSolutionExplorerFace.PreferTreeHost(page);

        MfdEditorHost.Visibility = showEditor ? Visibility.Visible : Visibility.Collapsed;
        if (MfdSolutionExplorerTree is not null)
            MfdSolutionExplorerTree.Visibility = showSe ? Visibility.Visible : Visibility.Collapsed;

        // SE Face: SoftOrgan EICAS band (opening… + clr/ack/list) is not the file tree.
        if (MfdEicasChrome is not null)
        {
            var onSe = string.Equals(page, "SolutionExplorer", StringComparison.OrdinalIgnoreCase);
            MfdEicasChrome.Visibility = onSe ? Visibility.Collapsed : Visibility.Visible;
        }

        var showTerminal = MfdTerminalHost is not null
            && string.Equals(page, "Terminal", StringComparison.OrdinalIgnoreCase);
        var showBuild = MfdBuildHost is not null
            && string.Equals(page, "Build", StringComparison.OrdinalIgnoreCase);
        var showTests = MfdTestsHost is not null
            && string.Equals(page, "Tests", StringComparison.OrdinalIgnoreCase);
        var showGlanceCards = IsGlanceCardsHostActive();
        // Terminal/Build/Tests host visibility owned by RefreshMfd*Visibility (called from UpdateMfdBody).
        MfdBody.Visibility = (showEditor || showSe || showTerminal || showBuild || showTests || showGlanceCards) ? Visibility.Collapsed : Visibility.Visible;
    }

    void TryOpenDogfoodFile()
    {
        var here = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location);
        if (string.IsNullOrWhiteSpace(here))
        {
            EditorPathLabel.Text = "(no assembly dir)";
            return;
        }

        var src = Path.GetFullPath(Path.Combine(here, "..", "..", "..", "MainWindow.xaml.cs"));
        if (!File.Exists(src))
        {
            EditorPathLabel.Text = "(dogfood MainWindow.xaml.cs not found)";
            return;
        }

        OpenCodeFile(src);
    }

    void OpenCodeFile(string path, int? line = null, int? lineEnd = null)
    {
        EnsureEditorChrome();
        CodeEditor.Load(path);
        GlassAvalonEditTheme.ApplyDarkReadable(CodeEditor);
        if (!_editorTextMate!.ApplyForPath(path))
            CodeEditor.SyntaxHighlighting = GlassAvalonEditTheme.ResolveDefinition(path);
        _editorChrome!.SetModeForPath(path);
        _editorPath = path;
        RefreshEditorSharedChrome();

        if (line is > 0)
        {
            SelectOpenDocumentLines(line.Value, lineEnd);
        }

        if (_session.IsIntercomForward)
        {
            MountEditor(MfdEditorHost);
            SelectMfdPage("Editor", sticky: true);
        }

        RefreshMfdEditorVisibility();
    }

    /// <summary>Select 1-based line range in the currently open AvalonEdit document (c:els / attach chips).</summary>
    internal void SelectOpenDocumentLines(int startLine, int? endLine = null)
    {
        if (CodeEditor?.Document is null || CodeEditor.Document.LineCount < 1)
        {
            StatusText.Text = "glass · c:els — no open document";
            return;
        }

        var max = CodeEditor.Document.LineCount;
        var start = Math.Clamp(startLine, 1, max);
        var end = endLine is > 0 ? Math.Clamp(endLine.Value, 1, max) : start;
        if (end < start)
            (start, end) = (end, start);

        var startDoc = CodeEditor.Document.GetLineByNumber(start);
        var endDoc = CodeEditor.Document.GetLineByNumber(end);
        CodeEditor.Select(startDoc.Offset, endDoc.EndOffset - startDoc.Offset);
        CodeEditor.ScrollToLine(start);
        CodeEditor.TextArea.Caret.BringCaretToView();
        StatusText.Text = end == start
            ? $"glass · c:els · L{start}"
            : $"glass · c:els · L{start}:{end}";

        if (_session.IsIntercomForward)
        {
            MountEditor(MfdEditorHost);
            SelectMfdPage("Editor", sticky: true);
        }

        RefreshMfdEditorVisibility();
    }

    void OpenFileBtn_OnClick(object sender, RoutedEventArgs e) => TryPickOpenFile();

    void SaveFileBtn_OnClick(object sender, RoutedEventArgs e) => TrySaveEditor();

    void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_openFamilyAwait && TryConsumeOpenFamilyKeyDown(e))
            return;

        if (_chordMelodyAwait && TryConsumeChordMelodyKeyDown(e))
            return;

        if (e.Key == Key.Escape && OpenFamilyOverlay?.Visibility == Visibility.Visible)
        {
            CloseOpenFamily();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && ChordOverlay?.Visibility == Visibility.Visible)
        {
            CloseCascadeChord();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && PaletteOverlay?.Visibility == Visibility.Visible)
        {
            CloseCommandPalette();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers != ModifierKeys.Control)
            return;

        // WPF reports Key.Q as Key.Q; Digits may differ — handle Oem-safe Q via KeyConverter path.
        if (e.Key == Key.Q)
        {
            ToggleCommandPalette();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.P)
        {
            OpenGoToFilePalette();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.K)
        {
            ToggleCascadeChord();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.S)
        {
            TrySaveEditor();
            e.Handled = true;
        }
        else if (e.Key == Key.O)
        {
            BeginOpenFamilyChord();
            e.Handled = true;
        }
    }

    void TrySaveEditor()
    {
        if (string.IsNullOrWhiteSpace(_editorPath))
        {
            StatusText.Text = "glass · save skipped · no file open";
            return;
        }

        try
        {
            CodeEditor.Save(_editorPath);
            PublishHumanDiskSave(_editorPath);
            StatusText.Text = $"glass · saved · {Path.GetFileName(_editorPath)} · {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"glass · save fail · {ex.Message}";
        }
    }
}
