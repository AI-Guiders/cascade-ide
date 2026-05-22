namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>DAP / отладка: делегирование регистрации хендлеров launch/attach и stepping.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterDapDebug(Action<string, Handler> add)
    {
        RegisterDapDebugLaunchAttach(add);
        RegisterDapDebugStepping(add);
    }
}
