#nullable enable

using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD Git — redirected git status TextBox peel (Avalonia GitMfdPageView SSOT).</summary>
public partial class MainWindow
{
    const int MaxGitChars = 200_000;

    GlassRedirectedGit? _gitRunner;
    readonly StringBuilder _gitBuffer = new();

    void RefreshMfdGitVisibility()
    {
        if (MfdGitHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "Git", StringComparison.OrdinalIgnoreCase);
        MfdGitHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && GitStatusLabel is not null && _gitRunner is not { IsRunning: true })
            GitStatusLabel.Text = "redirected · idle";
    }

    bool IsGitHostActive()
    {
        if (MfdGitHost is null)
            return false;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "Git", StringComparison.OrdinalIgnoreCase)
               && MfdGitHost.Visibility == Visibility.Visible;
    }

    void DisposeGitSession()
    {
        if (_gitRunner is null)
            return;

        _gitRunner.TextReceived -= OnGitText;
        _gitRunner.Dispose();
        _gitRunner = null;
    }

    void OnGitText(string chunk) =>
        Dispatcher.BeginInvoke(() => AppendGitText(chunk));

    void AppendGitText(string chunk)
    {
        if (GitOutput is null || string.IsNullOrEmpty(chunk))
            return;

        _gitBuffer.Append(chunk);
        if (_gitBuffer.Length > MaxGitChars)
            _gitBuffer.Remove(0, _gitBuffer.Length - MaxGitChars);

        GitOutput.Text = _gitBuffer.ToString();
        GitOutput.CaretIndex = GitOutput.Text.Length;
        GitOutput.ScrollToEnd();
    }

    internal void GitStatus_OnClick(object sender, RoutedEventArgs e)
    {
        if (_gitRunner is { IsRunning: true })
            return;

        DisposeGitSession();
        _gitBuffer.Clear();
        if (GitOutput is not null)
            GitOutput.Text = "";

        _gitRunner = new GlassRedirectedGit();
        _gitRunner.TextReceived += OnGitText;
        _gitRunner.Exited += code =>
            Dispatcher.BeginInvoke(() =>
            {
                AppendGitText($"\n┌ exited · {code} ┐\n");
                if (GitStatusLabel is not null)
                    GitStatusLabel.Text = $"redirected · done · {code}";
            });

        try
        {
            if (GitStatusLabel is not null)
                GitStatusLabel.Text = "redirected · git";
            _gitRunner.Start(_session.WorkspaceRoot ?? Environment.CurrentDirectory);
            if (GitStatusLabel is not null)
                GitStatusLabel.Text = $"redirected · {_gitRunner.DisplayTarget}";
        }
        catch (Exception ex)
        {
            AppendGitText($"┌ start fail · {ex.Message} ┐\n");
            if (GitStatusLabel is not null)
                GitStatusLabel.Text = "redirected · fail";
        }
    }

    internal void GitCancel_OnClick(object sender, RoutedEventArgs e) =>
        _gitRunner?.Cancel();

    internal void GitClear_OnClick(object sender, RoutedEventArgs e)
    {
        _gitBuffer.Clear();
        if (GitOutput is not null)
            GitOutput.Text = "";
        if (GitStatusLabel is not null && _gitRunner is not { IsRunning: true })
            GitStatusLabel.Text = "redirected · idle";
    }
}
