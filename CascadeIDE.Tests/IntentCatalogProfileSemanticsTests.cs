using System.Text.Json;
using CascadeIDE.Services;
using Json.Schema;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>Profile <c>cide-intent-catalog</c>: bundled catalog is structurally valid and loads with prod semantics.</summary>
public sealed class IntentCatalogProfileSemanticsTests
{
    [Fact]
    public void Bundled_catalog_matches_json_schema()
    {
        Assert.True(File.Exists(IntentCatalogProfileSupport.SchemaPath), IntentCatalogProfileSupport.SchemaPath);

        var schema = JsonSchema.FromFile(IntentCatalogProfileSupport.SchemaPath);
        var document = IntentCatalogProfileSupport.ToJsonNode(IntentCatalogProfileSupport.ReadSourceCatalogText());
        var element = document.Deserialize<JsonElement>();

        var result = schema.Evaluate(element, new EvaluationOptions { OutputFormat = OutputFormat.List });
        Assert.True(result.IsValid, FormatSchemaErrors(result));
    }

    [Fact]
    public void Bundled_catalog_loads_via_prod_loader()
    {
        IntentMelodyAliases.ResetForTests();
        var snapshot = IntentMelodyAliases.GetCatalogSnapshot();

        Assert.NotEmpty(snapshot.SlashRoutes);
        Assert.True(snapshot.Roots.Count + snapshot.SlashRoutes.Count > 0);
    }

    [Fact]
    public void Bundled_catalog_command_ids_are_registered_in_IdeCommands()
    {
        IntentMelodyAliases.ResetForTests();
        var snapshot = IntentMelodyAliases.GetCatalogSnapshot();
        var unknown = IntentCatalogProfileSupport.CollectCommandIds(snapshot)
            .Where(id => !IntentCatalogProfileSupport.TryGetIdeCommandSummary(id, out _))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unknown.Count == 0,
            "command_id not in IdeCommands contract: " + string.Join(", ", unknown));
    }

    [Fact]
    public void Source_catalog_declares_schema_directive()
    {
        using var reader = File.OpenText(IntentCatalogProfileSupport.SourceCatalogPath);
        for (var i = 0; i < 8 && !reader.EndOfStream; i++)
        {
            var line = reader.ReadLine();
            if (line is null)
                break;

            if (line.Contains("#:schema", StringComparison.OrdinalIgnoreCase))
                return;
        }

        Assert.Fail("Missing #:schema directive in IntentMelody/intent-catalog.toml");
    }

    private static string FormatSchemaErrors(EvaluationResults result)
    {
        var lines = new List<string>();
        WalkErrors(result, lines);
        return lines.Count == 0 ? "JSON Schema validation failed." : string.Join(Environment.NewLine, lines);
    }

    private static void WalkErrors(EvaluationResults node, List<string> lines)
    {
        if (node.Errors is { Count: > 0 })
        {
            var location = string.IsNullOrWhiteSpace(node.EvaluationPath.ToString())
                ? "$"
                : node.EvaluationPath.ToString();
            foreach (var (key, message) in node.Errors)
                lines.Add($"{location}: {key} — {message}");
        }

        if (node.Details is null)
            return;

        foreach (var child in node.Details)
            WalkErrors(child, lines);
    }
}
