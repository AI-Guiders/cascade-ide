#nullable enable

using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD Build — redirected dotnet build log + MSBuild problems ListBox.</summary>
public partial class MainWindow
{
    const int MaxBuildChars = 200_000;

    GlassRedirectedBuild? _buildRunner;
    readonly StringBuilder _buildBuffer = new();
    readonly ObservableCollection<GlassProblemItem> _buildProblems = new();

    void RefreshMfdBuildVisibility()
    {
        if (MfdBuildHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "Build", StringComparison.OrdinalIgnoreCase);
        MfdBuildHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && BuildProblemsList is not null && !ReferenceEquals(BuildProblemsList.ItemsSource, _buildProblems))
            BuildProblemsList.ItemsSource = _buildProblems;

        if (show && BuildStatusLabel is not null && _buildRunner is not { IsRunning: true })
            BuildStatusLabel.Text = "redirected · idle";
    }

    bool IsBuildHostActive()
    {
        if (MfdBuildHost is null)
            return false;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "Build", StringComparison.OrdinalIgnoreCase)
               && MfdBuildHost.Visibility == Visibility.Visible;
    }

    void DisposeBuildSession()
    {
        if (_buildRunner is null)
            return;

        _buildRunner.TextReceived -= OnBuildText;
        _buildRunner.Dispose();
        _buildRunner = null;
    }

    void OnBuildText(string chunk) =>
        Dispatcher.BeginInvoke(() => AppendBuildText(chunk));

    void AppendBuildText(string chunk)
    {
        if (BuildOutput is null || string.IsNullOrEmpty(chunk))
            return;

        _buildBuffer.Append(chunk);
        if (_buildBuffer.Length > MaxBuildChars)
            _buildBuffer.Remove(0, _buildBuffer.Length - MaxBuildChars);

        BuildOutput.Text = _buildBuffer.ToString();
        BuildOutput.CaretIndex = BuildOutput.Text.Length;
        BuildOutput.ScrollToEnd();
        RefreshBuildProblems();
    }

    void RefreshBuildProblems()
    {
        _buildProblems.Clear();
        foreach (var row in GlassProblemsMsBuildParse.Parse(_buildBuffer.ToString()))
            _buildProblems.Add(row);
    }

    internal void BuildRun_OnClick(object sender, RoutedEventArgs e)
    {
        if (_buildRunner is { IsRunning: true })
            return;

        DisposeBuildSession();
        _buildBuffer.Clear();
        _buildProblems.Clear();
        if (BuildOutput is not null)
            BuildOutput.Text = "";

        _buildRunner = new GlassRedirectedBuild();
        _buildRunner.TextReceived += OnBuildText;
        _buildRunner.Exited += code =>
            Dispatcher.BeginInvoke(() =>
            {
                AppendBuildText($"\n┌ exited · {code} ┐\n");
                RefreshBuildProblems();
                RefreshEditorSituRibbon();
                if (BuildStatusLabel is not null)
                {
                    var err = _buildProblems.Count(p => p.IsError);
                    var warn = _buildProblems.Count - err;
                    BuildStatusLabel.Text = $"redirected · done · {code} · {err} err · {warn} warn";
                }
            });

        try
        {
            if (BuildStatusLabel is not null)
                BuildStatusLabel.Text = "redirected · building";
            _buildRunner.Start(_session.WorkspaceRoot ?? Environment.CurrentDirectory);
            if (BuildStatusLabel is not null)
                BuildStatusLabel.Text = $"redirected · {_buildRunner.DisplayTarget}";
        }
        catch (Exception ex)
        {
            AppendBuildText($"┌ start fail · {ex.Message} ┐\n");
            if (BuildStatusLabel is not null)
                BuildStatusLabel.Text = "redirected · fail";
        }
    }

    internal void BuildCancel_OnClick(object sender, RoutedEventArgs e)
    {
        _buildRunner?.Cancel();
        if (BuildStatusLabel is not null)
            BuildStatusLabel.Text = "redirected · cancel";
    }

    internal void BuildClear_OnClick(object sender, RoutedEventArgs e)
    {
        _buildBuffer.Clear();
        _buildProblems.Clear();
        if (BuildOutput is not null)
            BuildOutput.Text = "";
    }
}
