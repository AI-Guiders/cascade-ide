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
    public void Get_supported_editor_languages_matches_EditorLanguageSupport_payload()
    {
        var expected = EditorLanguageSupport.GetJson();
        var actual = AgentContractRunner.GetContractJson(IdeCommands.GetSupportedEditorLanguages);
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

    [Fact]
    public async Task Git_status_uses_workspace_and_returns_json_shape()
    {
        var tmp = Directory.CreateTempSubdirectory("cascade_agent_contract_git_");
        try
        {
            await RunGitAsync(tmp.FullName, "init");
            File.WriteAllText(Path.Combine(tmp.FullName, "a.txt"), "x");
            await RunGitAsync(tmp.FullName, "add", "a.txt");
            await RunGitAsync(tmp.FullName, "-c", "user.email=t@t", "-c", "user.name=t", "commit", "-m", "init");

            var json = AgentContractRunner.GetContractJson(
                ["--workspace", tmp.FullName, IdeCommands.GitStatus]);
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("success", out var ok));
            Assert.True(ok.GetBoolean());
            Assert.True(doc.RootElement.TryGetProperty("exit_code", out var code));
            Assert.Equal(0, code.GetInt32());
            Assert.True(doc.RootElement.TryGetProperty("output", out var output));
            Assert.False(string.IsNullOrEmpty(output.GetString()));
        }
        finally
        {
            try
            {
                Directory.Delete(tmp.FullName, recursive: true);
            }
            catch
            {
                // best effort
            }
        }
    }

    private static async Task RunGitAsync(string workingDirectory, params string[] args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);
        using var p = System.Diagnostics.Process.Start(psi);
        Assert.NotNull(p);
        await p.WaitForExitAsync().ConfigureAwait(false);
        Assert.Equal(0, p.ExitCode);
    }
}
