namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>MCP-хендлеры меню и тулбара: делегирование по группам (диалоги, тема/язык, панели и сборка).</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterMenuAndToolbarCommands(Action<string, Handler> add)
    {
        RegisterMenuToolbarDialogsAndHosts(add);
        RegisterMenuToolbarThemeAndLanguage(add);
        RegisterMenuToolbarPanelsLayoutAndBuild(add);
    }
}
