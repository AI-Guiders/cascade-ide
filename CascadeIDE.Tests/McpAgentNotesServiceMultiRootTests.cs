using System.Text;
using AgentNotes.Core;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class McpAgentNotesServiceMultiRootTests
{
    [Fact]
    public void ReadKnowledgeFile_UsesReadOnlyRoot_ByKnowledgeRootId()
    {
        using var primary = CreatePrimaryRoot();
        using var group = CreateGroupKbRoot();
        using var scope = InstallRuntime(primary.Path, group.Path, out var settings);
        var svc = new McpAgentNotesService(() => settings);

        var text = svc.ReadKnowledgeFile("group/smoke-test-v1.md", knowledgeRootId: "group");
        Assert.Contains("group-kb smoke", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteKnowledgeFile_ToReadOnlyRoot_ReturnsError()
    {
        using var primary = CreatePrimaryRoot();
        using var group = CreateGroupKbRoot();
        using var scope = InstallRuntime(primary.Path, group.Path, out var settings);
        var svc = new McpAgentNotesService(() => settings);

        var result = svc.WriteKnowledgeFile("group/evil.md", "nope", knowledgePath: null, saveRevision: false, knowledgeRootId: "group");
        Assert.Contains("read-only", result, StringComparison.OrdinalIgnoreCase);
    }

    private static PrimaryRoot CreatePrimaryRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "CascadeIdeAnTests", "primary", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(path, "knowledge", "work", "local"));
        File.WriteAllText(Path.Combine(path, "agent-notes.md"), "# hot\n", Encoding.UTF8);
        File.WriteAllText(Path.Combine(path, "knowledge", "work", "local", "workspace-scope-map-v1.md"), "# map\n", Encoding.UTF8);
        return new PrimaryRoot(path);
    }

    private static GroupKbRoot CreateGroupKbRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "CascadeIdeAnTests", "group", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(path, "knowledge", "group"));
        File.WriteAllText(
            Path.Combine(path, "knowledge", "group", "smoke-test-v1.md"),
            "# Group KB smoke\n\ngroup-kb smoke test content.\n",
            Encoding.UTF8);
        File.WriteAllText(Path.Combine(path, "agent-notes.md"), "# Group hot (stub)\n", Encoding.UTF8);
        return new GroupKbRoot(path);
    }

    private static RuntimeScope InstallRuntime(string primaryPath, string groupPath, out CascadeIdeSettings settings)
    {
        var tomlPath = Path.Combine(Path.GetTempPath(), "CascadeIdeAnTests", $"cfg-{Guid.NewGuid():N}.toml");
        Directory.CreateDirectory(Path.GetDirectoryName(tomlPath)!);
        File.WriteAllText(
            tomlPath,
            $"""
            version = 1

            [knowledge]
            primary = "test"

            [knowledge.roots]
            test = "{primaryPath.Replace('\\', '/')}"

            [[knowledge.read_only]]
            id = "group"
            path = "{groupPath.Replace('\\', '/')}"

            [workspace]
            default_scope = "door-to-singularity"
            scope_map = "work/local/workspace-scope-map-v1.md"
            scope_aliases = "work/local/scope-alias-map-v1.md"

            [status]
            enabled = false
            port = 17341
            bind = "127.0.0.1"
            """,
            Encoding.UTF8);

        AgentNotesRuntimeLoader.Reset();
        settings = new CascadeIdeSettings { AgentNotes = new AgentNotesSettings { ConfigPath = tomlPath } };
        Assert.True(AgentNotesRuntimeLoader.EnsureInitialized(settings));
        return new RuntimeScope(tomlPath);
    }

    private sealed class PrimaryRoot(string path) : IDisposable
    {
        internal string Path { get; } = path;

        public void Dispose() => TryDelete(Path);
    }

    private sealed class GroupKbRoot(string path) : IDisposable
    {
        internal string Path { get; } = path;

        public void Dispose() => TryDelete(Path);
    }

    private sealed class RuntimeScope(string tomlPath) : IDisposable
    {
        public void Dispose()
        {
            AgentNotesRuntimeLoader.Reset();
            TryDelete(tomlPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
            else if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}
