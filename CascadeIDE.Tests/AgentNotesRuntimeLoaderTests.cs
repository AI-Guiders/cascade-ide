using AgentNotes.Core;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AgentNotesRuntimeLoaderTests
{
    [Fact]
    public void EnsureInitialized_loads_primary_root_from_config_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "cascade-anl-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path.Combine(root, "knowledge", "work", "local"));
        var tomlPath = Path.Combine(root, "agent-notes.toml");
        File.WriteAllText(
            tomlPath,
            $"""
            version = 1
            [knowledge]
            primary = "test"
            [knowledge.roots]
            test = "{root.Replace('\\', '/')}"
            [workspace]
            default_scope = "door-to-singularity"
            scope_map = "work/local/workspace-scope-map-v1.md"
            scope_aliases = "work/local/scope-alias-map-v1.md"
            """);
        File.WriteAllText(Path.Combine(root, "knowledge", "work", "local", "workspace-scope-map-v1.md"), "# map\n");

        try
        {
            var settings = new CascadeIdeSettings { AgentNotes = new AgentNotesSettings { ConfigPath = tomlPath } };
            Assert.True(AgentNotesRuntimeLoader.EnsureInitialized(settings));
            Assert.True(AgentNotesRuntime.TryGetPrimaryKnowledgeRoot(out var primary));
            Assert.Equal(Path.GetFullPath(root), Path.GetFullPath(primary));
        }
        finally
        {
            AgentNotesRuntimeLoader.Reset();
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // best-effort
            }
        }
    }
}
