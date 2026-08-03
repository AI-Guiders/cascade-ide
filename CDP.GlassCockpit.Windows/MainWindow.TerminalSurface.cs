#nullable enable

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD Terminal — shared ConPTY session (VT control depth OPEN).</summary>
public partial class MainWindow
{
    const int MaxTerminalChars = 120_000;

    GlassConPtyShell? _terminalShell;
    readonly StringBuilder _terminalBuffer = new();

    void EnsureTerminalSession()
    {
        if (_terminalShell is { IsRunning: true })
            return;

        _terminalShell?.Dispose();
        _terminalShell = new GlassConPtyShell();
        _terminalShell.TextReceived += OnTerminalText;
        _terminalShell.Exited += code =>
            Dispatcher.BeginInvoke(() => AppendTerminalText($"\n┌ exited · {code} ┐\n"));

        try
        {
            _terminalShell.Start(_session.WorkspaceRoot ?? Environment.CurrentDirectory);
            if (TerminalShellLabel is not null)
                TerminalShellLabel.Text = $"conpty · {_terminalShell.DisplayName}";
        }
        catch (Exception ex)
        {
            AppendTerminalText($"┌ start fail · {ex.Message} ┐\n");
            if (TerminalShellLabel is not null)
                TerminalShellLabel.Text = "conpty · fail";
        }
    }

    void DisposeTerminalSession()
    {
        if (_terminalShell is null)
            return;

        _terminalShell.TextReceived -= OnTerminalText;
        _terminalShell.Dispose();
        _terminalShell = null;
    }

    void OnTerminalText(string chunk) =>
        Dispatcher.BeginInvoke(() => AppendTerminalText(chunk));

    void AppendTerminalText(string chunk)
    {
        if (TerminalOutput is null || string.IsNullOrEmpty(chunk))
            return;

        _terminalBuffer.Append(chunk);
        if (_terminalBuffer.Length > MaxTerminalChars)
            _terminalBuffer.Remove(0, _terminalBuffer.Length - MaxTerminalChars);

        TerminalOutput.Text = _terminalBuffer.ToString();
        TerminalOutput.CaretIndex = TerminalOutput.Text.Length;
        TerminalOutput.ScrollToEnd();
    }

    void RefreshMfdTerminalVisibility()
    {
        if (MfdTerminalHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "Terminal", StringComparison.OrdinalIgnoreCase);
        MfdTerminalHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show)
            EnsureTerminalSession();
    }

    bool IsTerminalHostActive()
    {
        if (MfdTerminalHost is null)
            return false;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "Terminal", StringComparison.OrdinalIgnoreCase)
               && MfdTerminalHost.Visibility == Visibility.Visible;
    }

    internal void TerminalInput_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || TerminalInput is null)
            return;

        e.Handled = true;
        var line = TerminalInput.Text ?? "";
        TerminalInput.Clear();
        EnsureTerminalSession();
        AppendTerminalText("> " + line + Environment.NewLine);
        _terminalShell?.SendLine(line);
    }

    internal void TerminalRestart_OnClick(object sender, RoutedEventArgs e)
    {
        DisposeTerminalSession();
        _terminalBuffer.Clear();
        if (TerminalOutput is not null)
            TerminalOutput.Text = "";
        EnsureTerminalSession();
    }
}
