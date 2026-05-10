using System.Text.Json;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: каталог инструментов <c>list_tools</c>.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private static void RegisterCore(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ListTools, async (_, _) =>
        {
            bool includeDebugTools = false;
#if DEBUG
            includeDebugTools = true;
#endif
            var tools = Services.IdeMcpToolCatalog.BuildTools(includeDebugTools);
            return await Task.FromResult(JsonSerializer.Serialize(tools));
        });
    }
}
