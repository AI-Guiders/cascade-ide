#nullable enable

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>files_desk-LATEST → MFD FilesDesk list Face (SE≠FM) + SoftKeys Up/Open/List.</summary>
public partial class MainWindow
{
    readonly ObservableCollection<LatchPaint.FilesDeskEntryView> _filesDeskRows = new();
    string? _filesDeskCwd;
    bool _filesDeskHandsWired;

    void InitFilesDeskFace()
    {
        if (MfdFilesDeskList is not null)
            MfdFilesDeskList.ItemsSource = _filesDeskRows;
        EnsureFilesDeskHandsWired();
        _latches.SoftOrganChanged += OnSoftOrganForFilesDesk;
        TryHydrateFilesDeskFace();
    }

    void EnsureFilesDeskHandsWired()
    {
        if (_filesDeskHandsWired)
            return;
        if (FilesDeskSoftKeys is not null)
        {
            FilesDeskSoftKeys.Key1Click += (_, _) => FilesDeskUp();
            FilesDeskSoftKeys.Key2Click += (_, _) => FilesDeskOpenSelected();
            FilesDeskSoftKeys.Key3Click += (_, _) => FilesDeskListCwd();
        }

        if (MfdFilesDeskList is not null)
        {
            MfdFilesDeskList.MouseDoubleClick += (_, e) =>
            {
                FilesDeskActivateSelected();
                e.Handled = true;
            };
            MfdFilesDeskList.PreviewKeyDown += MfdFilesDeskList_OnPreviewKeyDown;
        }

        _filesDeskHandsWired = true;
    }

    void MfdFilesDeskList_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return))
            return;
        FilesDeskActivateSelected();
        e.Handled = true;
    }

    void OnSoftOrganForFilesDesk(string organId, string? _)
    {
        if (!organId.Equals("files_desk", StringComparison.OrdinalIgnoreCase))
            return;
        Dispatcher.BeginInvoke(TryHydrateFilesDeskFace, DispatcherPriority.Background);
    }

    void TryHydrateFilesDeskFace()
    {
        try
        {
            var path = CdpHabitatPaths.GetLatchPath("files_desk-LATEST.json");
            if (!File.Exists(path))
                return;
            ApplyFilesDeskLatch(path);
        }
        catch
        {
            /* best-effort */
        }
    }

    void ApplyFilesDeskLatch(string path)
    {
        var raw = File.ReadAllText(path);
        var view = LatchPaint.PaintFilesDesk(raw);
        if (view is null)
            return;

        if (!string.IsNullOrWhiteSpace(view.Cwd) && Directory.Exists(view.Cwd))
            _filesDeskCwd = view.Cwd;

        _filesDeskRows.Clear();
        foreach (var row in view.Entries)
            _filesDeskRows.Add(row);

        PaintFilesDeskStatus(view);
        RefreshFilesDeskVisibility();
    }

    void PaintFilesDeskStatus(LatchPaint.FilesDeskView? view = null)
    {
        if (!string.Equals(CurrentMfdPage(), "FilesDesk", StringComparison.OrdinalIgnoreCase))
            return;

        var cwd = _filesDeskCwd ?? view?.Cwd ?? "—";
        var status = view?.StatusLine ?? $"files · local · {cwd} · {_filesDeskRows.Count}";
        if (MfdBody is not null && _filesDeskRows.Count == 0)
        {
            MfdBody.Text = view is { Active: true }
                ? $"{status}\n{view.Pulse ?? ""}\nop={view.Op ?? "—"} · SoftKeys: Up / Open / List"
                : $"files · idle · cwd={cwd}\nSoftKeys: Up / Open / List · DoubleClick/Enter opens";
        }

        StatusText.Text = $"glass · {status} · {DateTime.Now:HH:mm:ss}";
    }

    void RefreshFilesDeskVisibility()
    {
        var on = string.Equals(CurrentMfdPage(), "FilesDesk", StringComparison.OrdinalIgnoreCase);
        if (MfdFilesDeskHost is not null)
            MfdFilesDeskHost.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        else if (MfdFilesDeskList is not null)
            MfdFilesDeskList.Visibility = on ? Visibility.Visible : Visibility.Collapsed;

        if (on && MfdBody is not null)
            MfdBody.Visibility = _filesDeskRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    void FilesDeskUp()
    {
        var cwd = ResolveFilesDeskCwd();
        var parent = Directory.GetParent(cwd)?.FullName;
        if (parent is null)
        {
            StatusText.Text = "glass · files · already at root";
            return;
        }

        EnumerateFilesDesk(parent, "up");
    }

    void FilesDeskListCwd()
    {
        EnumerateFilesDesk(ResolveFilesDeskCwd(), "list");
    }

    void FilesDeskOpenSelected() => FilesDeskActivateSelected();

    void FilesDeskActivateSelected()
    {
        if (MfdFilesDeskList?.SelectedItem is not LatchPaint.FilesDeskEntryView row)
            return;

        var path = row.Path;
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusText.Text = "glass · files · no path on row";
            return;
        }

        if (row.Kind.Equals("dir", StringComparison.OrdinalIgnoreCase) || Directory.Exists(path))
        {
            EnumerateFilesDesk(path, "cd");
            return;
        }

        if (!File.Exists(path))
        {
            StatusText.Text = $"glass · files · missing {path}";
            return;
        }

        OpenCodeFile(path);
        StatusText.Text = $"glass · files · open · {Path.GetFileName(path)} · {DateTime.Now:HH:mm:ss}";
    }

    string ResolveFilesDeskCwd()
    {
        if (!string.IsNullOrWhiteSpace(_filesDeskCwd) && Directory.Exists(_filesDeskCwd))
            return _filesDeskCwd!;
        if (!string.IsNullOrWhiteSpace(_session.WorkspaceRoot) && Directory.Exists(_session.WorkspaceRoot))
            return _session.WorkspaceRoot!;
        return Environment.CurrentDirectory;
    }

    void EnumerateFilesDesk(string cwd, string op)
    {
        try
        {
            if (!Directory.Exists(cwd))
            {
                StatusText.Text = $"glass · files · missing dir {cwd}";
                return;
            }

            _filesDeskCwd = Path.GetFullPath(cwd);
            _filesDeskRows.Clear();
            const int cap = 200;
            var infos = new DirectoryInfo(_filesDeskCwd)
                .EnumerateFileSystemInfos()
                .OrderBy(x => x is not DirectoryInfo)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Take(cap);
            foreach (var info in infos)
            {
                var kind = info is DirectoryInfo ? "dir" : "file";
                var mark = kind == "dir" ? "[dir]" : "[file]";
                _filesDeskRows.Add(new LatchPaint.FilesDeskEntryView(
                    kind, info.Name, info.FullName, $"{mark} {info.Name}"));
            }

            PaintFilesDeskStatus(new LatchPaint.FilesDeskView(
                Active: true,
                Pulse: $"files · {op} · {_filesDeskCwd}",
                Op: op,
                Where: "local",
                Cwd: _filesDeskCwd,
                EntryCount: _filesDeskRows.Count,
                Entries: _filesDeskRows.ToList(),
                StatusLine: $"files · local · {_filesDeskCwd} · {_filesDeskRows.Count}"));
            RefreshFilesDeskVisibility();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"glass · files · {op} fail · {ex.Message}";
        }
    }
}
