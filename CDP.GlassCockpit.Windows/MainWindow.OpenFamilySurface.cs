#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.SoftOrgan;
using Microsoft.Win32;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass Open/Load SoftFL — Ctrl+O → F/P/D/R (CIDE open file/sln/folder + recent).</summary>
public partial class MainWindow
{
    sealed record OpenFamilyRow(string Id, string Title, string Help, string? Path = null);

    readonly ObservableCollection<OpenFamilyRow> _openFamilyRows = new();
    DispatcherTimer? _openFamilyTimeout;
    bool _openFamilyAwait;
    bool _openFamilyRecentMode;

    void InitOpenFamily()
    {
        if (OpenFamilyList is null)
            return;
        OpenFamilyList.ItemsSource = _openFamilyRows;
        OpenFamilyList.MouseDoubleClick += (_, _) => ExecuteOpenFamilySelection();
        OpenFamilyList.PreviewKeyDown += OpenFamilyList_OnPreviewKeyDown;
    }

    void BeginOpenFamilyChord()
    {
        CloseCommandPalette();
        CloseCascadeChord();
        _openFamilyAwait = true;
        _openFamilyRecentMode = false;
        ShowOpenFamilyChoices();
        if (OpenFamilyOverlay is not null)
            OpenFamilyOverlay.Visibility = Visibility.Visible;
        Focus();
        ArmOpenFamilyTimeout();
        StatusText.Text = "open · F файл · P проект · D папка · R недавние";
    }

    void CloseOpenFamily()
    {
        _openFamilyAwait = false;
        _openFamilyRecentMode = false;
        DisarmOpenFamilyTimeout();
        _openFamilyRows.Clear();
        if (OpenFamilyOverlay is not null)
            OpenFamilyOverlay.Visibility = Visibility.Collapsed;
    }

    void ArmOpenFamilyTimeout()
    {
        DisarmOpenFamilyTimeout();
        _openFamilyTimeout = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _openFamilyTimeout.Tick += (_, _) =>
        {
            if (!_openFamilyAwait || _openFamilyRecentMode)
                return;
            CloseOpenFamily();
            TryPickOpenFile();
        };
        _openFamilyTimeout.Start();
    }

    void DisarmOpenFamilyTimeout()
    {
        if (_openFamilyTimeout is null)
            return;
        _openFamilyTimeout.Stop();
        _openFamilyTimeout = null;
    }

    void ShowOpenFamilyChoices()
    {
        if (OpenFamilyTitle is not null)
            OpenFamilyTitle.Text = "Open · F файл · P проект · D папка · R недавние";
        _openFamilyRows.Clear();
        _openFamilyRows.Add(new("file", "F · файл", "OpenFileDialog → AvalonEdit"));
        _openFamilyRows.Add(new("project", "P · проект / решение", "*.sln · *.slnx · *.csproj → workspace"));
        _openFamilyRows.Add(new("folder", "D · папка", "OpenFolderDialog → workspace root"));
        _openFamilyRows.Add(new("recent", "R · недавние", "MRU file / project / folder"));
        if (OpenFamilyList is not null)
            OpenFamilyList.SelectedIndex = 0;
    }

    void ShowOpenFamilyRecent()
    {
        CloseCommandPalette();
        CloseCascadeChord();
        _openFamilyAwait = true;
        _openFamilyRecentMode = true;
        DisarmOpenFamilyTimeout();
        if (OpenFamilyTitle is not null)
            OpenFamilyTitle.Text = "Open · недавние · Enter / Esc";
        _openFamilyRows.Clear();
        foreach (var e in GlassOpenRecentStore.List())
        {
            var exists = File.Exists(e.Path) || Directory.Exists(e.Path);
            if (!exists)
                continue;
            _openFamilyRows.Add(new(
                "recent_item",
                $"{e.Kind} · {Path.GetFileName(e.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}",
                e.Path,
                e.Path));
        }

        if (_openFamilyRows.Count == 0)
            _openFamilyRows.Add(new("empty", "Пусто", "Ещё ничего не открывали через Open-family"));

        if (OpenFamilyList is not null)
            OpenFamilyList.SelectedIndex = 0;
        if (OpenFamilyOverlay is not null)
            OpenFamilyOverlay.Visibility = Visibility.Visible;
        Focus();
        StatusText.Text = $"open · recent · {_openFamilyRows.Count} · {DateTime.Now:HH:mm:ss}";
    }

    void OpenFamilyList_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseOpenFamily();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ExecuteOpenFamilySelection();
            e.Handled = true;
        }
    }

    bool TryConsumeOpenFamilyKeyDown(KeyEventArgs e)
    {
        if (!_openFamilyAwait || OpenFamilyOverlay?.Visibility != Visibility.Visible)
            return false;

        if (e.Key == Key.Escape)
        {
            CloseOpenFamily();
            e.Handled = true;
            return true;
        }

        if (_openFamilyRecentMode)
        {
            if (e.Key == Key.Enter)
            {
                ExecuteOpenFamilySelection();
                e.Handled = true;
                return true;
            }

            return false;
        }

        if (Keyboard.Modifiers != ModifierKeys.None)
            return false;

        switch (e.Key)
        {
            case Key.F:
                CloseOpenFamily();
                TryPickOpenFile();
                e.Handled = true;
                return true;
            case Key.P:
                CloseOpenFamily();
                TryPickOpenProject();
                e.Handled = true;
                return true;
            case Key.D:
                CloseOpenFamily();
                TryPickOpenFolder();
                e.Handled = true;
                return true;
            case Key.R:
                ShowOpenFamilyRecent();
                e.Handled = true;
                return true;
            case Key.Enter:
                ExecuteOpenFamilySelection();
                e.Handled = true;
                return true;
            default:
                return false;
        }
    }

    void ExecuteOpenFamilySelection()
    {
        if (OpenFamilyList?.SelectedItem is not OpenFamilyRow row)
        {
            if (_openFamilyRows.Count == 0)
                return;
            row = _openFamilyRows[0];
        }

        if (row.Id == "empty")
        {
            CloseOpenFamily();
            return;
        }

        if (row.Id == "recent_item" && !string.IsNullOrWhiteSpace(row.Path))
        {
            CloseOpenFamily();
            OpenRecentPath(row.Path);
            return;
        }

        CloseOpenFamily();
        switch (row.Id)
        {
            case "file":
                TryPickOpenFile();
                break;
            case "project":
                TryPickOpenProject();
                break;
            case "folder":
                TryPickOpenFolder();
                break;
            case "recent":
                ShowOpenFamilyRecent();
                break;
        }
    }

    void TryPickOpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open file · Glass",
            Filter =
                "Code|*.cs;*.xaml;*.csproj;*.fsproj;*.json;*.md;*.toml;*.txt;*.ps1;*.py|" +
                "Solution|*.sln;*.slnx;*.slnf|" +
                "All|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        ApplyOpenInitialDirectory(dlg);

        if (dlg.ShowDialog(this) != true)
            return;

        var path = dlg.FileName;
        var ext = Path.GetExtension(path);
        if (ext is ".sln" or ".slnx" or ".slnf" or ".csproj" or ".fsproj")
        {
            ApplyWorkspaceFromProjectPath(path);
            return;
        }

        OpenCodeFile(path);
        GlassOpenRecentStore.Remember(path, "file");
        StatusText.Text = $"open · file · {Path.GetFileName(path)} · {DateTime.Now:HH:mm:ss}";
    }

    void TryPickOpenProject()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open solution / project · Glass",
            Filter =
                "Solution / project|*.slnx;*.sln;*.slnf;*.csproj;*.fsproj|" +
                "All|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        ApplyOpenInitialDirectory(dlg);

        if (dlg.ShowDialog(this) != true)
            return;

        ApplyWorkspaceFromProjectPath(dlg.FileName);
    }

    void TryPickOpenFolder()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Open folder as workspace · Glass",
            Multiselect = false,
        };

        if (!string.IsNullOrWhiteSpace(_session.WorkspaceRoot) && Directory.Exists(_session.WorkspaceRoot))
            dlg.InitialDirectory = _session.WorkspaceRoot;

        if (dlg.ShowDialog(this) != true || string.IsNullOrWhiteSpace(dlg.FolderName))
            return;

        ApplyWorkspaceRoot(dlg.FolderName, "folder");
    }

    void OpenRecentPath(string path)
    {
        if (Directory.Exists(path))
        {
            ApplyWorkspaceRoot(path, "folder");
            return;
        }

        if (!File.Exists(path))
        {
            StatusText.Text = $"open · recent missing · {path}";
            return;
        }

        var ext = Path.GetExtension(path);
        if (ext is ".sln" or ".slnx" or ".slnf" or ".csproj" or ".fsproj")
        {
            ApplyWorkspaceFromProjectPath(path);
            return;
        }

        OpenCodeFile(path);
        GlassOpenRecentStore.Remember(path, "file");
        StatusText.Text = $"open · recent file · {Path.GetFileName(path)} · {DateTime.Now:HH:mm:ss}";
    }

    void ApplyOpenInitialDirectory(OpenFileDialog dlg)
    {
        if (!string.IsNullOrWhiteSpace(_editorPath) && File.Exists(_editorPath))
            dlg.InitialDirectory = Path.GetDirectoryName(_editorPath);
        else if (!string.IsNullOrWhiteSpace(_session.WorkspaceRoot) && Directory.Exists(_session.WorkspaceRoot))
            dlg.InitialDirectory = _session.WorkspaceRoot;
    }

    void ApplyWorkspaceFromProjectPath(string path)
    {
        // CIDE LoadSolution(path): SolutionPath = file; workspace dir = WorkspaceDirectoryFromSolutionPath.
        if (!_session.SetSolutionOrProjectPath(path))
        {
            StatusText.Text = $"open · project fail · {path}";
            return;
        }

        GlassOpenRecentStore.Remember(path, "project");
        SelectMfdPage("SolutionExplorer", sticky: true);
        UpdateMfdBody();
        var rootName = Path.GetFileName((_session.WorkspaceRoot ?? "").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        StatusText.Text =
            $"open · project · {Path.GetFileName(path)} · root={rootName} · {DateTime.Now:HH:mm:ss}";
    }

    void ApplyWorkspaceRoot(string root, string kind)
    {
        if (!_session.SetWorkspaceRoot(root))
        {
            StatusText.Text = $"open · workspace fail · {root}";
            return;
        }

        GlassOpenRecentStore.Remember(root, kind);
        SelectMfdPage("SolutionExplorer", sticky: true);
        UpdateMfdBody();
        StatusText.Text = $"open · {kind} · {root} · {DateTime.Now:HH:mm:ss}";
    }
}
