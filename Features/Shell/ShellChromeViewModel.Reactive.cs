using CascadeIDE.Models;

namespace CascadeIDE.Features.Shell;

/// <summary>Side-effects shell-состояния на host (настройки, навигация MFD, bloom UI mode).</summary>
public sealed partial class ShellChromeViewModel
{
    partial void OnUiModeChanged(string value) => _host.HandleShellUiModeChanged(value);

    partial void OnIsPfdRegionExpandedChanged(bool value) => _host.HandleShellIsPfdRegionExpandedChanged(value);

    partial void OnIsTerminalVisibleChanged(bool value) => _host.HandleShellIsTerminalVisibleChanged(value);

    partial void OnIsBuildOutputVisibleChanged(bool value) => _host.HandleShellIsBuildOutputVisibleChanged(value);

    partial void OnIsInstrumentationDockVisibleChanged(bool value) =>
        _host.HandleShellIsInstrumentationDockVisibleChanged(value);

    partial void OnIsMfdRegionExpandedChanged(bool value) => _host.HandleShellIsMfdRegionExpandedChanged(value);

    partial void OnIsGitPanelVisibleChanged(bool value) => _host.HandleShellIsGitPanelVisibleChanged(value);

    partial void OnCurrentMfdShellPageChanged(MfdShellPage value) => _host.HandleShellCurrentMfdShellPageChanged(value);
}
