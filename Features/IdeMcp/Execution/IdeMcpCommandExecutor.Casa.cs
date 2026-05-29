using CascadeIDE.Features.CasaField.Application;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Execution;

internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterCasa(Action<string, Handler> add)
    {
        add(IdeCommands.CasaFieldQuery, async (args, _) =>
        {
            var query = McpCommandJsonArgs.String(args, "query")?.Trim();
            if (string.IsNullOrEmpty(query))
                return """{"error":"missing query"}""";

            var open = McpCommandJsonArgs.OptionalBool(args, "open") == true;
            var json = CasaFieldQueryResolver.BuildJson(_vm.GetWorkspacePath(), query);

            if (open)
            {
                var result = CasaFieldQueryResolver.Query(_vm.GetWorkspacePath(), query);
                if (result.Targets.Count > 0)
                    CasaFieldNavigationOpener.TryOpenFirstTarget(_vm, result);
            }

            return json;
        });
    }
}
