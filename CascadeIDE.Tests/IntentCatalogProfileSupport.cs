using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using CascadeIDE.Services;
using Tomlyn;
using Tomlyn.Model;

namespace CascadeIDE.Tests;

internal static class IntentCatalogProfileSupport
{
    internal static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    internal static string SourceCatalogPath =>
        Path.Combine(RepoRoot, "IntentMelody", "intent-catalog.toml");

    internal static string SchemaPath =>
        Path.Combine(RepoRoot, "docs", "schemas", "intent-catalog.schema.json");

    internal static string ReadSourceCatalogText() =>
        File.ReadAllText(SourceCatalogPath);

    internal static JsonNode ToJsonNode(string tomlText)
    {
        var table = TomlSerializer.Deserialize<TomlTable>(tomlText)
            ?? throw new InvalidOperationException("Empty TOML document.");
        return TomlTableToJson.Convert(table);
    }

    internal static bool TryGetIdeCommandSummary(string commandId, out string summary)
    {
        var docType = typeof(IdeCommands).Assembly.GetType("CascadeIDE.Services.IdeCommandsDoc", throwOnError: true)!;
        var method = docType.GetMethod("TryGetSummary", BindingFlags.Public | BindingFlags.Static)!;
        object?[] args = [commandId, null];
        var ok = (bool)method.Invoke(null, args)!;
        summary = (string?)args[1] ?? "";
        return ok;
    }

    internal static IReadOnlyCollection<string> CollectCommandIds(IntentMelodyCatalogSnapshot snapshot)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in snapshot.Roots.Values)
        {
            if (!string.IsNullOrWhiteSpace(root.CommandId))
                ids.Add(root.CommandId.Trim());
        }

        foreach (var route in snapshot.SlashRoutes.Values)
        {
            if (!string.IsNullOrWhiteSpace(route.CommandId))
                ids.Add(route.CommandId.Trim());
        }

        return ids;
    }

    private static class TomlTableToJson
    {
        internal static JsonObject Convert(TomlTable table)
        {
            var obj = new JsonObject();
            foreach (var (key, value) in table)
                obj[key] = ConvertValue(value);
            return obj;
        }

        private static JsonArray ConvertTableArray(TomlTableArray array)
        {
            var arr = new JsonArray();
            foreach (var table in array)
                arr.Add(Convert(table));
            return arr;
        }

        private static JsonArray ConvertArray(TomlArray array)
        {
            var arr = new JsonArray();
            foreach (var item in array)
                arr.Add(ConvertValue(item));
            return arr;
        }

        private static JsonNode? ConvertValue(object? value) =>
            value switch
            {
                null => null,
                TomlTable t => Convert(t),
                TomlTableArray ta => ConvertTableArray(ta),
                TomlArray a => ConvertArray(a),
                string s => JsonValue.Create(s),
                bool b => JsonValue.Create(b),
                long l => JsonValue.Create(l),
                int i => JsonValue.Create(i),
                double d => JsonValue.Create(d),
                float f => JsonValue.Create(f),
                DateTimeOffset dto => JsonValue.Create(dto.ToString("O")),
                DateTime dt => JsonValue.Create(dt.ToString("O")),
                _ => JsonValue.Create(value.ToString()),
            };
    }
}
