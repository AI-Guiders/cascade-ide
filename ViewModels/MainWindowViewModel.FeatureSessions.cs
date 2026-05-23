using CascadeIDE.Features.Build;
using CascadeIDE.Features.Debug;
using CascadeIDE.Features.Shell;

namespace CascadeIDE.ViewModels;

/// <summary>Wave 2 этап 5: application shell, debug, build session VMs.</summary>
public partial class MainWindowViewModel
{
    public MainWindowApplicationShellViewModel ApplicationShell { get; private set; } = null!;

    public MainWindowDebugSessionViewModel Debug { get; private set; } = null!;

    public MainWindowBuildSessionViewModel Build { get; private set; } = null!;

    internal Task ShowDebugInfoAsync(string title, string message) =>
        RequestShowInfoAsync != null ? RequestShowInfoAsync(title, message) : Task.CompletedTask;
}
