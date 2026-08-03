#nullable enable

using System.Text;
using System.Windows;
using System.Windows.Controls;
using CascadeIDE.Features.Terminal.DataAcquisition;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Glass MFD Terminal — EasyWindowsTerminalControl (WT WPF VT).
/// Avalonia EOL: cabin takes ready WPF terminal; launch cmdline from GlassCore IntegratedShellLaunch.
/// </summary>
public partial class MainWindow
{
    bool _terminalStarted;
    string _terminalDisplayName = "?";

    void EnsureTerminalSession()
    {
        if (TerminalVt is null)
            return;

        if (_terminalStarted)
            return;

        try
        {
            var cwd = IntegratedShellLaunch.ResolveWorkingDirectory(
                _session.WorkspaceRoot ?? Environment.CurrentDirectory);
            var launch = IntegratedShellLaunch.ResolveLaunchConfiguration(cwd);
            _terminalDisplayName = launch.DisplayName;
            TerminalVt.StartupCommandLine = BuildStartupCommandLine(launch);
            TerminalVt.RestartTerm();
            _terminalStarted = true;
            if (TerminalShellLabel is not null)
                TerminalShellLabel.Text = $"vt · {_terminalDisplayName} · {launch.WorkingDirectory}";
        }
        catch (Exception ex)
        {
            _terminalStarted = false;
            if (TerminalShellLabel is not null)
                TerminalShellLabel.Text = $"vt · fail · {ex.Message}";
        }
    }

    void DisposeTerminalSession()
    {
        if (TerminalVt is null)
            return;

        try
        {
            TerminalVt.DisconnectConPTYTerm();
        }
        catch
        {
            // best-effort
        }

        _terminalStarted = false;
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

    internal void TerminalRestart_OnClick(object sender, RoutedEventArgs e)
    {
        DisposeTerminalSession();
        EnsureTerminalSession();
    }

    static string BuildStartupCommandLine(ShellLaunchConfiguration launch)
    {
        var sb = new StringBuilder();
        QuoteArg(sb, launch.FileName);
        foreach (var arg in launch.Arguments)
        {
            sb.Append(' ');
            QuoteArg(sb, arg);
        }

        return sb.ToString();
    }

    static void QuoteArg(StringBuilder sb, string arg)
    {
        if (arg.Length == 0)
        {
            sb.Append("\"\"");
            return;
        }

        var needsQuotes = arg.Contains(' ') || arg.Contains('\t') || arg.Contains('"');
        if (!needsQuotes)
        {
            sb.Append(arg);
            return;
        }

        sb.Append('"');
        foreach (var ch in arg)
        {
            if (ch == '"')
                sb.Append('\\');
            sb.Append(ch);
        }

        sb.Append('"');
    }
}
