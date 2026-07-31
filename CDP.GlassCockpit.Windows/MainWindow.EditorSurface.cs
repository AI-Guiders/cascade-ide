#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;

namespace CDP.GlassCockpit.Windows;

/// <summary>AvalonEdit surface — mount, dogfood open, pick/save, Ctrl+O/S.</summary>
public partial class MainWindow
{
    string? _editorPath;

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
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = editorOnM && string.Equals(page, "Editor", StringComparison.OrdinalIgnoreCase);
        MfdEditorHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        MfdBody.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
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

    void OpenCodeFile(string path)
    {
        CodeEditor.Load(path);
        CodeEditor.SyntaxHighlighting =
            HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path))
            ?? HighlightingManager.Instance.GetDefinition("C#");
        GlassAvalonEditTheme.ApplyDarkReadable(CodeEditor);
        _editorPath = path;
        EditorPathLabel.Text = path;

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
            StatusText.Text = $"glass · saved · {Path.GetFileName(_editorPath)} · {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"glass · save fail · {ex.Message}";
        }
    }
}
