#nullable enable

using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>Узкий контекст shell для <see cref="IdeMcpCommandExecutor"/> (фасад над <see cref="MainWindowViewModel"/>).</summary>
internal interface IMainWindowMcpHostContext
{
    MainWindowViewModel Vm { get; }
}
