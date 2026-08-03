#nullable enable

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD Problems — full ListBox host (MSBuild/dotnet feed; Roslyn in-proc later).</summary>
public partial class MainWindow
{
    readonly ObservableCollection<GlassProblemItem> _problemItems = new();
    Process? _problemsBuild;
    readonly StringBuilder _problemsBuildBuf = new();
    bool _problemsRefreshing;

    void RefreshMfdProblemsVisibility()
    {
        if (MfdProblemsHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "Problems", StringComparison.OrdinalIgnoreCase);
        MfdProblemsHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && ProblemsList is not null && !ReferenceEquals(ProblemsList.ItemsSource, _problemItems))
            ProblemsList.ItemsSource = _problemItems;

        if (show)
            RefreshProblemsSummary();
    }

    bool IsProblemsHostActive()
    {
        if (MfdProblemsHost is null)
            return false;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "Problems", StringComparison.OrdinalIgnoreCase)
               && MfdProblemsHost.Visibility == Visibility.Visible;
    }

    void RefreshProblemsSummary()
    {
        if (ProblemsStatusLabel is null)
            return;

        var errors = 0;
        var warns = 0;
        foreach (var p in _problemItems)
        {
            if (p.IsError) errors++;
            else warns++;
        }

        ProblemsStatusLabel.Text = _problemsRefreshing
            ? "problems · building…"
            : $"{errors} ошиб., {warns} предупр., всего {_problemItems.Count}";
    }

    internal void ProblemsRefresh_OnClick(object sender, RoutedEventArgs e) => StartProblemsRefresh();

    internal void ProblemsClear_OnClick(object sender, RoutedEventArgs e)
    {
        CancelProblemsBuild();
        _problemItems.Clear();
        RefreshProblemsSummary();
    }

    internal void ProblemsList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProblemsList?.SelectedItem is not GlassProblemItem item)
            return;
        if (!System.IO.File.Exists(item.FilePath))
        {
            StatusText.Text = $"glass · problems · missing {item.FileName}";
            return;
        }

        OpenCodeFile(item.FilePath, item.Line);
        StatusText.Text = $"glass · problems · {item.HeaderLine}";
    }

    void StartProblemsRefresh()
    {
        if (_problemsRefreshing)
            return;

        CancelProblemsBuild();
        _problemsBuildBuf.Clear();
        _problemItems.Clear();
        _problemsRefreshing = true;
        RefreshProblemsSummary();

        var cwd = _session.WorkspaceRoot ?? Environment.CurrentDirectory;
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build --nologo -v:q",
            WorkingDirectory = cwd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            p.OutputDataReceived += (_, args) =>
            {
                if (args.Data is { } s)
                    lock (_problemsBuildBuf) _problemsBuildBuf.AppendLine(s);
            };
            p.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is { } s)
                    lock (_problemsBuildBuf) _problemsBuildBuf.AppendLine(s);
            };
            p.Exited += (_, _) => Dispatcher.BeginInvoke(ApplyProblemsBuildResult, DispatcherPriority.Background);
            _problemsBuild = p;
            if (!p.Start())
                throw new InvalidOperationException("dotnet build failed to start");
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            _problemsRefreshing = false;
            _problemItems.Clear();
            _problemItems.Add(new GlassProblemItem(cwd, 1, 1, "error", "GLASS", ex.Message));
            RefreshProblemsSummary();
            StatusText.Text = $"glass · problems · {ex.Message}";
        }
    }

    void ApplyProblemsBuildResult()
    {
        string text;
        lock (_problemsBuildBuf)
            text = _problemsBuildBuf.ToString();

        var rows = GlassProblemsMsBuildParse.Parse(text);
        _problemItems.Clear();
        foreach (var r in rows)
            _problemItems.Add(r);

        _problemsRefreshing = false;
        RefreshProblemsSummary();
        StatusText.Text = $"glass · problems · refreshed · {_problemItems.Count}";
        DisposeProblemsBuildProcess();
    }

    void CancelProblemsBuild()
    {
        try
        {
            if (_problemsBuild is { HasExited: false })
                _problemsBuild.Kill(entireProcessTree: true);
        }
        catch
        {
            /* ignore */
        }

        DisposeProblemsBuildProcess();
        _problemsRefreshing = false;
    }

    void DisposeProblemsBuildProcess()
    {
        try { _problemsBuild?.Dispose(); } catch { /* ignore */ }
        _problemsBuild = null;
    }
}
