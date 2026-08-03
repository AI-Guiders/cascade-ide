#nullable enable

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD Git — porcelain ListBox + diff TextBox (Avalonia GitPanel parity v1).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassGitPorcelainParse.Row> _gitRows = new();
    Process? _gitProc;
    bool _gitBusy;

    void RefreshMfdGitVisibility()
    {
        if (MfdGitHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "Git", StringComparison.OrdinalIgnoreCase);
        MfdGitHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && GitList is not null && !ReferenceEquals(GitList.ItemsSource, _gitRows))
            GitList.ItemsSource = _gitRows;

        if (show && !_gitBusy && _gitRows.Count == 0)
            StartGitRefresh();
    }

    bool IsGitHostActive()
    {
        if (MfdGitHost is null)
            return false;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "Git", StringComparison.OrdinalIgnoreCase)
               && MfdGitHost.Visibility == Visibility.Visible;
    }

    internal void GitStatus_OnClick(object sender, RoutedEventArgs e) => StartGitRefresh();

    internal void GitCancel_OnClick(object sender, RoutedEventArgs e) => CancelGitProc();

    internal void GitClear_OnClick(object sender, RoutedEventArgs e)
    {
        CancelGitProc();
        _gitRows.Clear();
        if (GitOutput is not null)
            GitOutput.Text = "";
        if (GitStatusLabel is not null)
            GitStatusLabel.Text = "git · idle";
    }

    internal void GitList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GitList?.SelectedItem is not GlassGitPorcelainParse.Row row || GitOutput is null)
            return;

        var cwd = _session.WorkspaceRoot ?? Environment.CurrentDirectory;
        var args = row.IsStaged
            ? $"diff --cached -- \"{row.Path}\""
            : $"diff -- \"{row.Path}\"";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"-C \"{cwd}\" {args}",
                WorkingDirectory = cwd,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null)
            {
                GitOutput.Text = "git diff failed to start";
                return;
            }

            var text = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(20_000);
            GitOutput.Text = string.IsNullOrWhiteSpace(text) ? "(no diff)" : text;
            if (GitStatusLabel is not null)
                GitStatusLabel.Text = $"git · diff · {row.Display}";
        }
        catch (Exception ex)
        {
            GitOutput.Text = ex.Message;
        }
    }

    void StartGitRefresh()
    {
        if (_gitBusy)
            return;

        CancelGitProc();
        _gitRows.Clear();
        if (GitOutput is not null)
            GitOutput.Text = "";
        _gitBusy = true;
        if (GitStatusLabel is not null)
            GitStatusLabel.Text = "git · porcelain…";

        var cwd = _session.WorkspaceRoot ?? Environment.CurrentDirectory;
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C \"{cwd}\" status --porcelain=v1 -u",
            WorkingDirectory = cwd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var buf = new StringBuilder();
            p.OutputDataReceived += (_, a) =>
            {
                if (a.Data is { } s)
                    lock (buf) buf.AppendLine(s);
            };
            p.ErrorDataReceived += (_, a) =>
            {
                if (a.Data is { } s)
                    lock (buf) buf.AppendLine(s);
            };
            p.Exited += (_, _) => Dispatcher.BeginInvoke(() =>
            {
                string text;
                lock (buf) text = buf.ToString();
                _gitRows.Clear();
                foreach (var row in GlassGitPorcelainParse.Parse(text))
                    _gitRows.Add(row);
                _gitBusy = false;
                if (GitStatusLabel is not null)
                    GitStatusLabel.Text = $"git · {_gitRows.Count} rows";
                try { p.Dispose(); } catch { /* ignore */ }
                if (ReferenceEquals(_gitProc, p))
                    _gitProc = null;
            }, DispatcherPriority.Background);
            _gitProc = p;
            if (!p.Start())
                throw new InvalidOperationException("git failed to start");
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            _gitBusy = false;
            if (GitStatusLabel is not null)
                GitStatusLabel.Text = "git · fail";
            if (GitOutput is not null)
                GitOutput.Text = ex.Message;
        }
    }

    void DisposeGitSession() => CancelGitProc();

    void CancelGitProc()
    {
        try
        {
            if (_gitProc is { HasExited: false })
                _gitProc.Kill(entireProcessTree: true);
        }
        catch
        {
            /* ignore */
        }

        try { _gitProc?.Dispose(); } catch { /* ignore */ }
        _gitProc = null;
        _gitBusy = false;
    }
}
