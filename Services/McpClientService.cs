using System.Collections.Concurrent;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace CascadeIDE.Services;

/// <summary>
/// Клиент к внешним MCP-серверам (tool calling) — автономный режим может списокировать и вызывать тулы.
/// Подключение пока реализовано только через stdio (StdioClientTransport).
/// </summary>
public sealed class McpClientService : IAsyncDisposable
{
    private sealed record ServerSpec(string Name, string Command, IReadOnlyList<string> Arguments, string ToolPrefix, bool Enabled);

    private readonly string _externalMcpServersJson;
    private readonly ConcurrentDictionary<string, McpClient> _clients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, (string ServerName, string ToolName)> _toolsByKey = new(StringComparer.OrdinalIgnoreCase);

    public McpClientService(string externalMcpServersJson)
    {
        _externalMcpServersJson = externalMcpServersJson ?? "[]";
    }

    public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        var specs = ParseSpecs();
        foreach (var spec in specs.Where(s => s.Enabled))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_clients.ContainsKey(spec.Name))
                continue;

            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = spec.Name,
                Command = spec.Command,
                Arguments = spec.Arguments.ToArray()
            });

            var client = await McpClient.CreateAsync(transport).ConfigureAwait(false);
            _clients[spec.Name] = client;

            // Index tool names for fast routing: <toolPrefix>.<toolName>
            foreach (var tool in await client.ListToolsAsync().ConfigureAwait(false))
            {
                var key = $"{spec.ToolPrefix}.{tool.Name}";
                _toolsByKey[key] = (spec.Name, tool.Name);
            }
        }
    }

    public async Task<IReadOnlyList<(string ToolKey, string Description)>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        // We don't have the full tool description without a reverse index; for now, return empty description.
        // (Автономный раннер всё равно отправляет модели список через `tools` ключи; описания — опционально.)
        return _toolsByKey.Keys.Select(k => (k, "")).ToList();
    }

    public async Task<string> CallToolAsync(string toolKey, IReadOnlyDictionary<string, object?>? arguments, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolKey))
            return "Error: empty toolKey.";

        await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);

        if (!_toolsByKey.TryGetValue(toolKey, out var routing))
            return $"Error: tool not found: {toolKey}";

        if (!_clients.TryGetValue(routing.ServerName, out var client))
            return $"Error: MCP server not connected: {routing.ServerName}";

        var args = arguments is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(arguments, StringComparer.OrdinalIgnoreCase);

        // MCP tools expect an object dictionary, values can be string/number/bool/null/arrays/objects.
        var result = await client.CallToolAsync(routing.ToolName, args).ConfigureAwait(false);
        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
        if (!string.IsNullOrEmpty(text))
            return text;

        // Fallback: return full content as JSON.
        return JsonSerializer.Serialize(result.Content, new JsonSerializerOptions { WriteIndented = false });
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var kv in _clients)
        {
            try { await kv.Value.DisposeAsync().ConfigureAwait(false); }
            catch { /* ignore */ }
        }
        _clients.Clear();
        _toolsByKey.Clear();
    }

    private IReadOnlyList<ServerSpec> ParseSpecs()
    {
        try
        {
            using var doc = JsonDocument.Parse(_externalMcpServersJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<ServerSpec>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;

                string? name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                string? command = el.TryGetProperty("command", out var c) ? c.GetString() : null;
                bool enabled = el.TryGetProperty("enabled", out var en) && en.ValueKind == JsonValueKind.True;
                string? toolPrefix = el.TryGetProperty("toolPrefix", out var tp) ? tp.GetString() : null;

                var args = new List<string>();
                if (el.TryGetProperty("arguments", out var a) && a.ValueKind == JsonValueKind.Array)
                {
                    foreach (var arg in a.EnumerateArray())
                    {
                        if (arg.ValueKind == JsonValueKind.String)
                            args.Add(arg.GetString() ?? "");
                    }
                }

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command))
                    continue;

                list.Add(new ServerSpec(
                    Name: name,
                    Command: command,
                    Arguments: args,
                    ToolPrefix: string.IsNullOrWhiteSpace(toolPrefix) ? name : toolPrefix,
                    Enabled: enabled));
            }
            return list;
        }
        catch
        {
            return [];
        }
    }
}
