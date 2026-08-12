#nullable enable

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CascadeIDE.SoftInstrument;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Glass MFD Problems — severity board (ERR/WARN/ALL) + jump list (Shared-SSOT quality).
/// </summary>
public partial class MainWindow
{
    readonly List<GlassProblemItem> _problemAll = new();
    readonly ObservableCollection<GlassProblemItem> _problemItems = new();
    Process? _problemsBuild;
    readonly StringBuilder _problemsBuildBuf = new();
    bool _problemsRefreshing;
    bool _problemsBoardWired;
    /// <summary>null = ALL · error · warning</summary>
    string? _problemsSeverityFilter;

    void RefreshMfdProblemsVisibility()
    {
        if (MfdProblemsHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "Problems", StringComparison.OrdinalIgnoreCase);
        MfdProblemsHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        EnsureProblemsBoardWired();

        if (show && ProblemsList is not null && !ReferenceEquals(ProblemsList.ItemsSource, _problemItems))
            ProblemsList.ItemsSource = _problemItems;

        if (show)
            PaintProblemsBoard();
    }

    bool IsProblemsHostActive()
    {
        if (MfdProblemsHost is null)
            return false;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "Problems", StringComparison.OrdinalIgnoreCase)
               && MfdProblemsHost.Visibility == Visibility.Visible;
    }

    void EnsureProblemsBoardWired()
    {
        if (_problemsBoardWired)
            return;
        if (ProblemsErrCard is not null)
            ProblemsErrCard.MouseLeftButtonUp += (_, _) => SetProblemsSeverityFilter("error");
        if (ProblemsWarnCard is not null)
            ProblemsWarnCard.MouseLeftButtonUp += (_, _) => SetProblemsSeverityFilter("warning");
        if (ProblemsAllCard is not null)
            ProblemsAllCard.MouseLeftButtonUp += (_, _) => SetProblemsSeverityFilter(null);
        _problemsBoardWired = true;
    }

    void SetProblemsSeverityFilter(string? filter)
    {
        _problemsSeverityFilter = filter;
        ApplyProblemsFilter();
        PaintProblemsBoard();
    }

    void ApplyProblemsFilter()
    {
        _problemItems.Clear();
        foreach (var p in _problemAll)
        {
            if (_problemsSeverityFilter is null
                || (string.Equals(_problemsSeverityFilter, "error", StringComparison.OrdinalIgnoreCase) && p.IsError)
                || (string.Equals(_problemsSeverityFilter, "warning", StringComparison.OrdinalIgnoreCase) && p.IsWarning))
                _problemItems.Add(p);
        }
    }

    void PaintProblemsBoard()
    {
        var errors = 0;
        var warns = 0;
        foreach (var p in _problemAll)
        {
            if (p.IsError) errors++;
            else warns++;
        }

        if (ProblemsErrCount is not null)
            ProblemsErrCount.Text = errors.ToString();
        if (ProblemsWarnCount is not null)
            ProblemsWarnCount.Text = warns.ToString();
        if (ProblemsAllCount is not null)
            ProblemsAllCount.Text = _problemAll.Count.ToString();

        HighlightProblemsCard(ProblemsErrCard, string.Equals(_problemsSeverityFilter, "error", StringComparison.OrdinalIgnoreCase), "#E05858", "#2E1A1A");
        HighlightProblemsCard(ProblemsWarnCard, string.Equals(_problemsSeverityFilter, "warning", StringComparison.OrdinalIgnoreCase), "#D7A33C", "#2A2618");
        HighlightProblemsCard(ProblemsAllCard, _problemsSeverityFilter is null, "#888888", "#1A1A1A");

        if (ProblemsStatusLabel is not null)
        {
            var filter = _problemsSeverityFilter ?? "all";
            ProblemsStatusLabel.Text = _problemsRefreshing
                ? "problems · building…"
                : $"problems · {filter} · show {_problemItems.Count}/{_problemAll.Count} · err {errors} · warn {warns}";
        }
    }

    static void HighlightProblemsCard(Border? card, bool selected, string accentHex, string bgHex)
    {
        if (card is null)
            return;
        card.BorderBrush = (Brush)new BrushConverter().ConvertFromString(selected ? accentHex : "#3A3A3A")!;
        card.BorderThickness = new Thickness(selected ? 2 : 1);
        card.Background = (Brush)new BrushConverter().ConvertFromString(bgHex)!;
    }

    internal void ProblemsRefresh_OnClick(object sender, RoutedEventArgs e) => StartProblemsRefresh();

    internal void ProblemsClear_OnClick(object sender, RoutedEventArgs e)
    {
        CancelProblemsBuild();
        _problemAll.Clear();
        _problemsSeverityFilter = null;
        ApplyProblemsFilter();
        PaintProblemsBoard();
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
        _problemAll.Clear();
        ApplyProblemsFilter();
        _problemsRefreshing = true;
        PaintProblemsBoard();

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
            _problemAll.Clear();
            _problemAll.Add(new GlassProblemItem(cwd, 1, 1, "error", "GLASS", ex.Message));
            ApplyProblemsFilter();
            PaintProblemsBoard();
            StatusText.Text = $"glass · problems · {ex.Message}";
        }
    }

    void ApplyProblemsBuildResult()
    {
        string text;
        lock (_problemsBuildBuf)
            text = _problemsBuildBuf.ToString();

        var rows = GlassProblemsMsBuildParse.Parse(text);
        if (!string.IsNullOrWhiteSpace(_editorPath) && CodeEditor is not null)
        {
            var roslyn = GlassRoslynDiagnosticsFeed.CollectForFile(_editorPath, CodeEditor.Text);
            rows = GlassRoslynDiagnosticsFeed.MergeDistinct(rows, roslyn).ToList();
        }

        _problemAll.Clear();
        _problemAll.AddRange(rows);
        ApplyProblemsFilter();
        _problemsRefreshing = false;
        PaintProblemsBoard();
        StatusText.Text = $"glass · problems · refreshed · {_problemAll.Count}";
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
