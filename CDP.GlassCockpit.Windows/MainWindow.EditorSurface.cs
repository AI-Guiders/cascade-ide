#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace CDP.GlassCockpit.Windows;

/// <summary>AvalonEdit surface — mount, dogfood open, pick/save, Ctrl+O/S.</summary>
public partial class MainWindow
{
    string? _editorPath;
    GlassAvalonEditChrome? _editorChrome;
    GlassAvalonEditTextMate? _editorTextMate;

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

    void MountEditor(ContentControl host)
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
        RefreshMfdEditorVisibility();
    }

    void RefreshMfdEditorVisibility()
    {
        if (MfdEditorHost is null || MfdBody is null)
            return;

        var editorOnM = ReferenceEquals(EditorChrome.Parent, MfdEditorHost);
        var page = CurrentMfdPage();
        var showEditor = editorOnM && string.Equals(page, "Editor", StringComparison.OrdinalIgnoreCase);
        var showSe = MfdSolutionExplorerTree is not null
            && string.Equals(page, "SolutionExplorer", StringComparison.OrdinalIgnoreCase)
            && MfdSolutionExplorerTree.Items.Count > 0;

        MfdEditorHost.Visibility = showEditor ? Visibility.Visible : Visibility.Collapsed;
        if (MfdSolutionExplorerTree is not null)
            MfdSolutionExplorerTree.Visibility = showSe ? Visibility.Visible : Visibility.Collapsed;

        var showTerminal = MfdTerminalHost is not null
            && string.Equals(page, "Terminal", StringComparison.OrdinalIgnoreCase);
        var showBuild = MfdBuildHost is not null
            && string.Equals(page, "Build", StringComparison.OrdinalIgnoreCase);
        var showTests = MfdTestsHost is not null
            && string.Equals(page, "Tests", StringComparison.OrdinalIgnoreCase);
        // Terminal/Build/Tests host visibility owned by RefreshMfd*Visibility (called from UpdateMfdBody).
        MfdBody.Visibility = (showEditor || showSe || showTerminal || showBuild || showTests) ? Visibility.Collapsed : Visibility.Visible;
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

    void OpenCodeFile(string path, int? line = null)
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
            var target = Math.Min(line.Value, Math.Max(1, CodeEditor.Document.LineCount));
            CodeEditor.ScrollToLine(target);
            CodeEditor.TextArea.Caret.Line = target;
            CodeEditor.TextArea.Caret.Column = 1;
            CodeEditor.TextArea.Caret.BringCaretToView();
        }

        if (_session.IsIntercomForward)
        {
            MountEditor(MfdEditorHost);
            SelectMfdPage("Editor");
        }

        RefreshMfdEditorVisibility();
    }

    void OpenFileBtn_OnClick(object sender, RoutedEventArgs e) => TryPickOpenFile();

    void SaveFileBtn_OnClick(object sender, RoutedEventArgs e) => TrySaveEditor();

    void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && ChordOverlay.Visibility == Visibility.Visible)
        {
            CloseCascadeChord();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && PaletteOverlay.Visibility == Visibility.Visible)
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
            TryPickOpenFile();
            e.Handled = true;
        }
    }

    void TryPickOpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open in Glass editor",
            Filter = "Code|*.cs;*.xaml;*.csproj;*.json;*.md;*.toml;*.txt|All|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        if (!string.IsNullOrWhiteSpace(_editorPath) && File.Exists(_editorPath))
            dlg.InitialDirectory = Path.GetDirectoryName(_editorPath);
        else if (!string.IsNullOrWhiteSpace(_session.WorkspaceRoot) && Directory.Exists(_session.WorkspaceRoot))
            dlg.InitialDirectory = _session.WorkspaceRoot;

        if (dlg.ShowDialog(this) == true)
            OpenCodeFile(dlg.FileName);
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
