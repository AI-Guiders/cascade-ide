using System.Text.Json;

namespace CascadeIDE.ViewModels;

/// <summary>Диспетчер MCP-команд IDE: разбор args и вызов <see cref="IIdeMcpActions"/> / UI-команд главного окна.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private readonly MainWindowViewModel _vm;
    private readonly Dictionary<string, Handler> _handlers;

    private delegate Task<string> Handler(IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken);

    public IdeMcpCommandExecutor(MainWindowViewModel vm)
    {
        _vm = vm;
        _handlers = BuildHandlers();
    }

    /// <summary>Чтение примитивов из словаря аргументов MCP (JSON).</summary>
    private static class JsonArgs
    {
        public static string? String(IReadOnlyDictionary<string, JsonElement>? args, string key) =>
            args is not null && args.TryGetValue(key, out var e) ? e.GetString() : null;

        public static int Int(IReadOnlyDictionary<string, JsonElement>? args, string key, int defaultValue = 0) =>
            args is not null && args.TryGetValue(key, out var e) && e.TryGetInt32(out var v) ? v : defaultValue;

        public static bool Bool(IReadOnlyDictionary<string, JsonElement>? args, string key, bool defaultValue = false) =>
            args is not null && args.TryGetValue(key, out var e) && (e.ValueKind is JsonValueKind.True or JsonValueKind.False)
                ? e.GetBoolean()
                : defaultValue;

        public static List<string>? StringList(IReadOnlyDictionary<string, JsonElement>? args, string key)
        {
            if (args is null || !args.TryGetValue(key, out var e) || e.ValueKind != JsonValueKind.Array)
                return null;
            var values = new List<string>();
            foreach (var item in e.EnumerateArray())
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    values.Add(value);
            }
            return values;
        }
    }

    private static string ParseAndShowDebugBreakpoints(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("breakpoints", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return "Missing breakpoints (array of { file_path, line })";
        var list = new List<(string, int)>();
        foreach (var item in arr.EnumerateArray())
        {
            if (!item.TryGetProperty("file_path", out var fp) || !item.TryGetProperty("line", out var ln))
                continue;
            var path = fp.GetString();
            if (string.IsNullOrEmpty(path))
                continue;
            list.Add((path, ln.GetInt32()));
        }
        actions.ShowDebugBreakpoints(list);
        return "OK";
    }

    private static string ParseAndShowDebugState(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        var stackFrames = new List<(string, string?, int)>();
        var variables = new List<(string, string)>();
        if (args is not null)
        {
            if (args.TryGetValue("stack_frames", out var sf) && sf.ValueKind == JsonValueKind.Array)
                foreach (var item in sf.EnumerateArray())
                    stackFrames.Add((
                        item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        item.TryGetProperty("file", out var f) ? f.GetString() : null,
                        item.TryGetProperty("line", out var l) ? l.GetInt32() : 0));
            if (args.TryGetValue("variables", out var v) && v.ValueKind == JsonValueKind.Array)
                foreach (var item in v.EnumerateArray())
                    if (item.TryGetProperty("name", out var vn) && item.TryGetProperty("value", out var vv))
                        variables.Add((vn.GetString() ?? "", vv.GetString() ?? ""));
        }
        actions.ShowDebugState(stackFrames, variables);
        return "OK";
    }

    /// <summary>Вход с MCP/агента маршалится на UI в <see cref="MainWindowViewModel"/> до вызова хендлеров; UI-операции выполнять напрямую без вложенного маршалинга.</summary>
    public async Task<string> ExecuteAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken)
    {
        if (_handlers.TryGetValue(commandId, out var handler))
            return await handler(args, cancellationToken);

        return $"Unknown command: {commandId}";

    }

    private Dictionary<string, Handler> BuildHandlers()
    {
        var map = new Dictionary<string, Handler>(StringComparer.Ordinal);

        void Add(string id, Handler h) => map.Add(id, h);

        RegisterCore(Add);
        RegisterGenerated(Add); // generated .g.cs adds pass-through handlers
        RegisterEditorAndSolution(Add);
        RegisterDebuggerBreakpoints(Add);
        RegisterPreviewAndConfirmation(Add);
        RegisterEditorStateAndContent(Add);
        RegisterEditAndNavigation(Add);
        RegisterOutputAndFocus(Add);
        // NOTE: these are now generated:
        // - workspace/solution info
        // - build/tests
        // - git
        // - output/build panel (toggle panels); focus_editor is registered above.
        RegisterUiVisibilityAndModes(Add);
        RegisterMenuAndToolbarCommands(Add);
        RegisterFocusPowerAndAgentActions(Add);
        RegisterDocuments(Add);
        // NOTE: UI inspection/control (pure IIdeMcpActions) is generated.
        RegisterDebugUiSurface(Add);
        RegisterAgentNotes(Add);

        return map;
    }

    // Generation hook: a source generator (or ProtocolDocGen) can emit a partial method
    // that registers 1:1 handlers for IIdeMcpActions methods.
    partial void RegisterGenerated(Action<string, Handler> add);
}
