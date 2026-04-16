using System.Text.Json;
using System.Text.Json.Nodes;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Services;
using CascadeIDE.Services.AgentContract;
using Xunit;

namespace CascadeIDE.Tests;

[Collection("UiModeCatalog")]
public sealed class AgentContractRunnerTests : IDisposable
{
    public AgentContractRunnerTests() =>
        UiModeCatalog.ResetForTests();

    public void Dispose() =>
        UiModeCatalog.ResetForTests();

    [Fact]
    public void Get_ui_modes_diagnostics_matches_UiModeCatalog_payload()
    {
        UiModeCatalog.Initialize();
        var expected = UiModeCatalog.GetDiagnosticsJson();
        var actual = AgentContractRunner.GetContractJson(IdeCommands.GetUiModesDiagnostics);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Get_ui_modes_diagnostics_normalized_paths_are_stable()
    {
        UiModeCatalog.Initialize();
        var raw = AgentContractRunner.GetContractJson(IdeCommands.GetUiModesDiagnostics);
        var normalized = NormalizeUiModesDiagnosticsPaths(raw);
        using var doc = JsonDocument.Parse(normalized);
        Assert.Equal("<BASE>", doc.RootElement.GetProperty("app_base_directory").GetString());
        Assert.StartsWith("<BASE>", doc.RootElement.GetProperty("ui_modes_directory").GetString(), StringComparison.Ordinal);
        Assert.StartsWith("<BASE>", doc.RootElement.GetProperty("index_toml_path").GetString(), StringComparison.Ordinal);
        Assert.True(doc.RootElement.GetProperty("ui_mode_catalog_initialized").GetBoolean());
    }

    /// <summary>Как в ADR 0052: убрать машинно-зависимые префиксы для снапшотов.</summary>
    internal static string NormalizeUiModesDiagnosticsPaths(string json)
    {
        var node = JsonNode.Parse(json)!;
        var baseDir = node["app_base_directory"]?.GetValue<string>();
        if (string.IsNullOrEmpty(baseDir))
            return json;

        var normalizedBase = baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        node["app_base_directory"] = "<BASE>";

        foreach (var key in new[] { "ui_modes_directory", "index_toml_path" })
        {
            if (node[key]?.GetValue<string>() is not { } p)
                continue;
            if (p.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            {
                var tail = p[normalizedBase.Length..].Replace('\\', '/');
                node[key] = "<BASE>" + tail;
            }
        }

        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }
}
