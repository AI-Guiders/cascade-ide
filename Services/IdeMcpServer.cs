using System.Text.Json;
using Cdp.Core;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Tool = ModelContextProtocol.Protocol.Tool;

namespace CascadeIDE.Services;

/// <summary>
/// MCP stdio host options + CDP phase×object catalog session (shared axes with Cursor <c>cdp-mcp</c>).
/// </summary>
public sealed class IdeMcpRuntime
{
    private readonly IIdeMcpActions _actions;
    private readonly List<Tool> _fullTools;
    private readonly IReadOnlyList<ToolAffordance> _affordances;
    private readonly Dictionary<string, Tool> _byName;
    private McpServer? _server;

    public SessionContext Session { get; } = new();
    public McpServerOptions Options { get; }

    private IdeMcpRuntime(IIdeMcpActions actions, List<Tool> fullTools, IReadOnlyList<ToolAffordance> affordances)
    {
        _actions = actions;
        _fullTools = fullTools;
        _affordances = affordances;
        _byName = fullTools.ToDictionary(t => t.Name, StringComparer.Ordinal);
        Options = BuildOptions();
    }

    public static IdeMcpRuntime Create(IIdeMcpActions actions)
    {
        bool includeDebugTools = false;
#if DEBUG
        includeDebugTools = true;
#endif
        var full = IdeMcpToolCatalog.BuildTools(includeDebugTools);
        return new IdeMcpRuntime(actions, full, IdeMcpAffordanceSeed.Build());
    }

    /// <summary>Catalog-only host for unit tests (no tool dispatch).</summary>
    internal static IdeMcpRuntime CreateForCatalogTests(bool includeDebugTools = false)
    {
        var full = IdeMcpToolCatalog.BuildTools(includeDebugTools);
        return new IdeMcpRuntime(null!, full, IdeMcpAffordanceSeed.Build());
    }

    /// <summary>Backward-compatible entry used by older call sites.</summary>
    public static McpServerOptions BuildOptions(IIdeMcpActions actions) => Create(actions).Options;

    public void AttachServer(McpServer server) => _server = server;

    private McpServerOptions BuildOptions() =>
        new()
        {
            ServerInfo = new Implementation { Name = "CascadeIDE", Version = "0.1.0" },
            ProtocolVersion = "2024-11-05",
            ServerInstructions =
                "Cascade IDE MCP. catalog=f(phase,object) via Cdp.Core (same axes as Cursor CDP). " +
                "Set ide_context then use shortlist; ide_tools peeks without changing session; " +
                "ide_execute_command remains escape hatch for any command_id.",
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability { ListChanged = true }
            },
            Handlers = new McpServerHandlers
            {
                ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = BuildVisibleTools() }),
                CallToolHandler = async (request, cancellationToken) =>
                {
                    var name = request.Params?.Name ?? "";
                    var args = request.Params?.Arguments is IReadOnlyDictionary<string, JsonElement> a ? a : null;
                    try
                    {
                        if (name is "ide_context" or "ide_tools")
                        {
                            var meta = DispatchMeta(name, args);
                            return new CallToolResult { Content = [new TextContentBlock { Text = meta }] };
                        }

                        var text = await CallToolByConventionAsync(_actions, name, args, cancellationToken)
                            .ConfigureAwait(false);
                        bool isError;
                        if (name == "ide_execute_command")
                            isError = text.StartsWith("Missing", StringComparison.Ordinal)
                                || text.StartsWith("Unknown command", StringComparison.Ordinal)
                                || text.StartsWith("Error", StringComparison.Ordinal);
                        else if (string.Equals(name, "ide_create_blank_solution", StringComparison.Ordinal))
                            isError = !text.StartsWith("OK:", StringComparison.Ordinal);
                        else
                        {
                            var isActionTool = name is "ide_open_file" or "ide_load_solution" or "ide_select" or "ide_set_breakpoint" or "ide_remove_breakpoint"
                                or "ide_show_preview" or "ide_show_editor_preview" or "ide_apply_edit" or "ide_go_to_position" or "ide_reveal_editor_range" or "ide_intercom_reveal_attachment" or "ide_focus_editor"
                                or "ide_set_ui_theme" or "ide_set_control_layout" or "ide_set_control_text" or "ide_click_control"
                                or "ide_send_keys" or "ide_set_focus" or "ide_highlight_control" or "ide_set_panel_size" or "ide_add_control"
                                or "ide_write_agent_notes"
                                or "ide_run_code_cleanup" or "ide_git_commit" or "ide_git_push"
                                or "ide_git_log" or "ide_git_fetch" or "ide_git_pull" or "ide_git_branch" or "ide_git_show" or "ide_git_submodule";
                            isError = isActionTool && text != "OK";
                        }
                        return new CallToolResult { Content = [new TextContentBlock { Text = text }], IsError = isError };
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("invalid thread", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var dir = AppContext.BaseDirectory;
                                var logPath = Path.Combine(dir, "invalid-thread-log.txt");
                                File.WriteAllText(logPath, ex.ToString());
                            }
                            catch { /* ignore */ }
#if DEBUG
                            var stack = ex.StackTrace ?? "";
                            return new CallToolResult { Content = [new TextContentBlock { Text = "Error: " + ex.Message + " [caught in IdeMcpServer]\n\n" + stack }], IsError = true };
#endif
                        }
                        return new CallToolResult { Content = [new TextContentBlock { Text = "Error: " + ex.Message }], IsError = true };
                    }
                }
            }
        };

    internal List<Tool> BuildVisibleTools()
    {
        var meta = BuildMetaTools();
        var hits = PhaseObjectCatalog.Query(
            _affordances, Session.Phase, Session.Object, Session.Intent, limit: 40, language: Session.Language);
        var domain = new List<Tool>();
        var seen = new HashSet<string>(meta.Select(t => t.Name), StringComparer.Ordinal);
        foreach (var hit in hits)
        {
            var name = hit.Affordance.PrefixedName;
            if (!seen.Add(name))
                continue;
            if (_byName.TryGetValue(name, out var tool))
                domain.Add(tool);
        }

        // Escape hatch always visible when present in full catalog.
        if (_byName.TryGetValue("ide_execute_command", out var exec) && seen.Add(exec.Name))
            domain.Add(exec);

        return meta.Concat(domain).ToList();
    }

    private List<Tool> BuildMetaTools() =>
    [
        Meta("ide_context", "Get/set CDP session phase+object(+intent[+language]). Same axes as Cursor cdp_context. Triggers tools/list_changed.", new
        {
            type = "object",
            properties = new
            {
                phase = new { type = "string", description = "explore|clarify|act|verify|handoff" },
                @object = new { type = "string", description = "kb|code|repo|task|finding|process|issue|session" },
                intent = new { type = "string", description = "optional find|cite|change|verify|record|ship" },
                language = new { type = "string", description = "optional any|csharp|python|delphi (cs|py|pas aliases); empty clears" },
                get = new { type = "boolean", description = "If true, only return current context." }
            }
        }),
        Meta("ide_tools", "Shortlist catalog=f(phase,object[,intent][,language]) without changing session (Cdp.Core).", new
        {
            type = "object",
            properties = new
            {
                phase = new { type = "string" },
                @object = new { type = "string" },
                intent = new { type = "string" },
                language = new { type = "string" },
                limit = new { type = "integer" }
            }
        })
    ];

    private static Tool Meta(string name, string desc, object schema) => new()
    {
        Name = name,
        Description = desc,
        InputSchema = JsonSerializer.SerializeToElement(schema)
    };

    private string DispatchMeta(string name, IReadOnlyDictionary<string, JsonElement>? args)
    {
        args ??= new Dictionary<string, JsonElement>();
        var pretty = new JsonSerializerOptions { WriteIndented = true };
        switch (name)
        {
            case "ide_context":
            {
                if (args.TryGetValue("get", out var g) && g.ValueKind == JsonValueKind.True)
                    return Session.ToJson();
                var changed = false;
                if (args.TryGetValue("phase", out var ph) && CdpEnumParse.TryParsePhase(ph.GetString(), out var newPhase))
                {
                    Session.Phase = newPhase;
                    changed = true;
                }
                if (args.TryGetValue("object", out var ob) && CdpEnumParse.TryParseObject(ob.GetString(), out var newObj))
                {
                    Session.Object = newObj;
                    changed = true;
                }
                if (args.TryGetValue("intent", out var it))
                {
                    var s = it.GetString();
                    if (string.IsNullOrWhiteSpace(s))
                        Session.Intent = null;
                    else if (CdpEnumParse.TryParseIntent(s, out var newIntent))
                        Session.Intent = newIntent;
                    changed = true;
                }
                if (args.TryGetValue("language", out var langEl))
                {
                    var ls = langEl.GetString();
                    if (string.IsNullOrWhiteSpace(ls))
                        Session.Language = null;
                    else if (CdpEnumParse.TryParseLanguage(ls, out var newLang))
                        Session.Language = CdpLanguages.IsAny(newLang) ? null : newLang;
                    changed = true;
                }
                if (changed && _server is not null)
                {
                    _ = _server.SendNotificationAsync(
                        NotificationMethods.ToolListChangedNotification,
                        cancellationToken: CancellationToken.None);
                }
                return Session.ToJson() + (changed ? "\n# list_changed: shortlist refreshed for new context" : "");
            }
            case "ide_tools":
            {
                var qPhase = Session.Phase;
                var qObj = Session.Object;
                CdpIntent? qIntent = Session.Intent;
                string? qLang = Session.Language;
                if (args.TryGetValue("phase", out var p2) && CdpEnumParse.TryParsePhase(p2.GetString(), out var pp))
                    qPhase = pp;
                if (args.TryGetValue("object", out var o2) && CdpEnumParse.TryParseObject(o2.GetString(), out var oo))
                    qObj = oo;
                if (args.TryGetValue("intent", out var i2) && CdpEnumParse.TryParseIntent(i2.GetString(), out var ii))
                    qIntent = ii;
                if (args.TryGetValue("language", out var l2) && CdpEnumParse.TryParseLanguage(l2.GetString(), out var ll))
                    qLang = CdpLanguages.IsAny(ll) ? null : ll;
                var limit = 40;
                if (args.TryGetValue("limit", out var lim) && lim.TryGetInt32(out var li))
                    limit = li;
                var hits = PhaseObjectCatalog.Query(_affordances, qPhase, qObj, qIntent, limit, qLang);
                return JsonSerializer.Serialize(new
                {
                    phase = CdpEnumParse.ToWire(qPhase),
                    @object = CdpEnumParse.ToWire(qObj),
                    intent = qIntent is null ? null : CdpEnumParse.ToWire(qIntent.Value),
                    language = qLang,
                    total = hits.Count,
                    full_catalog_count = _fullTools.Count,
                    tools = hits.Select(h => new
                    {
                        name = h.Affordance.PrefixedName,
                        score = h.Score,
                        cost = h.Affordance.Cost,
                        risk = h.Affordance.Risk
                    })
                }, pretty);
            }
            default:
                throw new ArgumentException($"Unknown meta tool: {name}");
        }
    }

    internal static async Task<string> CallToolByConventionAsync(
        IIdeMcpActions actions,
        string toolName,
        IReadOnlyDictionary<string, JsonElement>? args,
        CancellationToken cancellationToken)
    {
        if (toolName == "ide_execute_command")
            return await CallExecuteCommand(actions, args, cancellationToken).ConfigureAwait(false);

        if (!toolName.StartsWith("ide_", StringComparison.Ordinal))
            return $"Unknown tool: {toolName}";

        if (!IdeMcpToolNaming.TryToCommandId(toolName, out var commandId))
            return $"Unknown tool: {toolName}";

        if (string.Equals(commandId, IdeCommands.Build, StringComparison.Ordinal))
            commandId = IdeCommands.BuildStructured;

        return await actions.ExecuteCommandAsync(commandId, args, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> CallExecuteCommand(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken)
    {
        var merged = IdeExecuteCommandArgs.MergeNestedArgs(args);
        var commandId = merged is not null && merged.TryGetValue("command_id", out var cid) ? cid.GetString() : null;
        if (string.IsNullOrEmpty(commandId))
            return "Missing command_id";
        return await actions.ExecuteCommandAsync(commandId, merged, cancellationToken);
    }
}

/// <summary>Facade kept for call sites / tests that still reference <see cref="IdeMcpServer"/>.</summary>
public static class IdeMcpServer
{
    public static IdeMcpRuntime Create(IIdeMcpActions actions) => IdeMcpRuntime.Create(actions);

    public static McpServerOptions BuildOptions(IIdeMcpActions actions) => IdeMcpRuntime.BuildOptions(actions);

    // Kept for IdeMcpServerDispatchTests (reflection).
    private static Task<string> CallToolByConventionAsync(
        IIdeMcpActions actions,
        string toolName,
        IReadOnlyDictionary<string, JsonElement>? args,
        CancellationToken cancellationToken) =>
        IdeMcpRuntime.CallToolByConventionAsync(actions, toolName, args, cancellationToken);
}
