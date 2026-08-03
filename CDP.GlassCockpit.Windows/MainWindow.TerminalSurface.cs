#nullable enable

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CascadeIDE.Features.Terminal.DataAcquisition;
using EasyWindowsTerminalControl;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Glass MFD Terminal — EasyWindowsTerminalControl (WT WPF VT).
/// Avalonia EOL: cabin takes ready WPF terminal; launch cmdline from GlassCore IntegratedShellLaunch.
/// </summary>
public partial class MainWindow
{
    bool _terminalStarted;
    string _terminalDisplayName = "?";
    bool _terminalSizeHooked;

    void EnsureTerminalSession()
    {
        if (TerminalVt is null)
            return;

        if (_terminalStarted)
        {
            FocusTerminalVt();
            return;
        }

        // HwndHost+ConPTY started at 0×0 (Collapsed) eats input — wait for real layout.
        if (TerminalVt.ActualWidth < 8 || TerminalVt.ActualHeight < 8)
        {
            if (!_terminalSizeHooked)
            {
                _terminalSizeHooked = true;
                TerminalVt.SizeChanged += TerminalVt_OnSizedForStart;
            }

            return;
        }

        StartTerminalNow();
    }

    void TerminalVt_OnSizedForStart(object sender, SizeChangedEventArgs e)
    {
        if (TerminalVt is null)
            return;

        if (e.NewSize.Width < 8 || e.NewSize.Height < 8)
            return;

        TerminalVt.SizeChanged -= TerminalVt_OnSizedForStart;
        _terminalSizeHooked = false;

        if (!_terminalStarted && IsTerminalHostActive())
            StartTerminalNow();
    }

    void StartTerminalNow()
    {
        if (TerminalVt is null || _terminalStarted)
            return;

        try
        {
            var cwd = IntegratedShellLaunch.ResolveWorkingDirectory(
                _session.WorkspaceRoot ?? Environment.CurrentDirectory);
            var launch = IntegratedShellLaunch.ResolveLaunchConfiguration(cwd);
            _terminalDisplayName = launch.DisplayName;
            TerminalVt.WorkingDirectory = launch.WorkingDirectory;
            TerminalVt.StartupCommandLine = BuildStartupCommandLine(launch);
            TerminalVt.InputCapture =
                EasyTerminalControl.INPUT_CAPTURE.TabKey | EasyTerminalControl.INPUT_CAPTURE.DirectionKeys;
            // Restart replaces any 0×0 auto-start from Loaded while Collapsed.
            _ = TerminalVt.RestartTerm();
            _terminalStarted = true;
            if (TerminalShellLabel is not null)
                TerminalShellLabel.Text = $"vt · {_terminalDisplayName} · {launch.WorkingDirectory}";
            FocusTerminalVt();
        }
        catch (Exception ex)
        {
            _terminalStarted = false;
            if (TerminalShellLabel is not null)
                TerminalShellLabel.Text = $"vt · fail · {ex.Message}";
        }
    }

    void FocusTerminalVt()
    {
        if (TerminalVt is null)
            return;

        TerminalVt.Focusable = true;
        TerminalVt.Focus();
        Keyboard.Focus(TerminalVt);
    }

    void DisposeTerminalSession()
    {
        if (TerminalVt is null)
            return;

        if (_terminalSizeHooked)
        {
            TerminalVt.SizeChanged -= TerminalVt_OnSizedForStart;
            _terminalSizeHooked = false;
        }

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
        {
            // After layout: size HWND, (re)start ConPTY, steal focus from Composer.
            Dispatcher.BeginInvoke(
                () =>
                {
                    EnsureTerminalSession();
                    FocusTerminalVt();
                },
                DispatcherPriority.Loaded);
        }
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
        Dispatcher.BeginInvoke(
            () =>
            {
                EnsureTerminalSession();
                FocusTerminalVt();
            },
            DispatcherPriority.Loaded);
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
