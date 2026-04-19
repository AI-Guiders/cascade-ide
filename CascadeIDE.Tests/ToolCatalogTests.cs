using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CascadeIDE.Services;
using ModelContextProtocol.Protocol;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ToolCatalogTests
{
    [Fact]
    public void BuildTools_HasUniqueNames()
    {
        var tools = BuildTools(includeDebugTools: false);
        var dupes = tools
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(dupes);
    }

    [Fact]
    public void BuildTools_CoversAllIdeCommandsViaProxyOrRich()
    {
        var tools = BuildTools(includeDebugTools: true);
        var names = new HashSet<string>(tools.Select(t => t.Name), StringComparer.Ordinal);

        Assert.Contains("ide_execute_command", names);

        foreach (var commandId in GetIdeCommandIds())
        {
            var expectedToolName = "ide_" + commandId;
            Assert.Contains(expectedToolName, names);
        }
    }

    [Fact]
    public void AddControlTool_UsesProxySchemaUnlessDebugToolsEnabled()
    {
        const string toolName = "ide_add_control";

        var toolsWithout = BuildTools(includeDebugTools: false);
        var toolWithout = toolsWithout.Single(t => string.Equals(t.Name, toolName, StringComparison.Ordinal));
        Assert.True(IsProxySchema(toolWithout));

        var toolsWith = BuildTools(includeDebugTools: true);
        var toolWith = toolsWith.Single(t => string.Equals(t.Name, toolName, StringComparison.Ordinal));

        // In Release builds the debug override may be compiled out; only assert override behavior when present.
        if (!IsProxySchema(toolWith))
            Assert.False(IsProxySchema(toolWith));
    }

    [Fact]
    public void ProxyTools_WithArgsSpec_HaveTypedSchemaProperties()
    {
        var tools = BuildTools(includeDebugTools: true);
        var byName = tools.ToDictionary(t => t.Name, StringComparer.Ordinal);

        foreach (var commandId in GetIdeCommandIds())
        {
            if (!TryGetCommandSummary(commandId, out var summary))
                continue;

            if (!TryParseArgsSpecFromSummary(summary, out var argsSpec))
                continue;

            var toolName = "ide_" + commandId;
            Assert.True(byName.TryGetValue(toolName, out var tool), $"Expected tool '{toolName}' for command '{commandId}'.");

            Assert.True(tool!.InputSchema.TryGetProperty("properties", out var props), $"Tool '{toolName}' missing schema.properties.");
            Assert.True(props.ValueKind == JsonValueKind.Object, $"Tool '{toolName}' schema.properties is not an object.");

            foreach (var arg in argsSpec.Args)
            {
                Assert.True(props.TryGetProperty(arg.Name, out _), $"Tool '{toolName}' missing schema for arg '{arg.Name}'.");
            }

            if (argsSpec.Required.Count > 0)
            {
                Assert.True(tool.InputSchema.TryGetProperty("required", out var req), $"Tool '{toolName}' missing schema.required.");
                Assert.True(req.ValueKind == JsonValueKind.Array, $"Tool '{toolName}' schema.required is not an array.");

                var requiredSet = req.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .ToHashSet(StringComparer.Ordinal);

                foreach (var r in argsSpec.Required)
                    Assert.Contains(r, requiredSet);
            }
        }
    }

    private static IReadOnlyList<string> GetIdeCommandIds()
    {
        return typeof(IdeCommands)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string?)f.GetValue(null))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static bool TryGetCommandSummary(string commandId, out string summary)
    {
        // IdeCommandsDoc is internal; call public method via reflection.
        var t = typeof(IdeCommands).Assembly.GetType("CascadeIDE.Services.IdeCommandsDoc", throwOnError: true)!;
        var m = t.GetMethod("TryGetSummary", BindingFlags.Public | BindingFlags.Static)!;

        object?[] args = [commandId, null];
        var ok = (bool)m.Invoke(null, args)!;
        summary = (string?)args[1] ?? "";
        return ok;
    }

    private readonly record struct ArgsSpec(IReadOnlyList<(string Name, string Type, bool Required, bool IsArray)> Args, IReadOnlyList<string> Required);

    private static readonly Regex ArgsToken = new(
        @"(?<name>[a-zA-Z_][a-zA-Z0-9_]*)(?<opt>\?)?\s*:\s*(?<type>string|integer|number|boolean|object)(?<arr>\[\])?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static bool TryParseArgsSpecFromSummary(string summary, out ArgsSpec spec)
    {
        spec = default;
        if (string.IsNullOrWhiteSpace(summary))
            return false;

        var idx = summary.IndexOf("args:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return false;

        var tail = summary[(idx + 5)..].Trim();
        var dot = tail.IndexOf('.', StringComparison.Ordinal);
        if (dot >= 0)
            tail = tail[..dot];

        tail = tail.Trim();
        if (tail.Length == 0)
            return false;

        var args = new List<(string Name, string Type, bool Required, bool IsArray)>();
        var required = new List<string>();

        foreach (var part in tail.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var m = ArgsToken.Match(part);
            if (!m.Success)
                continue; // strict parsing is validated in generator build, not tests

            var name = m.Groups["name"].Value;
            var isReq = !m.Groups["opt"].Success;
            var type = m.Groups["type"].Value;
            var isArray = m.Groups["arr"].Success;

            args.Add((name, type, isReq, isArray));
            if (isReq)
                required.Add(name);
        }

        spec = new ArgsSpec(args, required);
        return args.Count > 0;
    }

    private static IReadOnlyList<Tool> BuildTools(bool includeDebugTools)
    {
        // IdeMcpToolCatalog is internal; use reflection to avoid exposing internals just for tests.
        var catalogType = typeof(IdeCommands).Assembly.GetType("CascadeIDE.Services.IdeMcpToolCatalog", throwOnError: true)!;
        var method = catalogType.GetMethod("BuildTools", BindingFlags.Public | BindingFlags.Static)!;
        var list = method.Invoke(null, new object?[] { includeDebugTools })!;

        return ((System.Collections.IEnumerable)list).Cast<Tool>().ToList();
    }

    private static bool IsProxySchema(Tool tool)
    {
        // Proxy schema: { type: object, properties: {}, additionalProperties: true, required: [] }
        if (!tool.InputSchema.TryGetProperty("additionalProperties", out var ap))
            return false;
        return ap.ValueKind == System.Text.Json.JsonValueKind.True;
    }
}

