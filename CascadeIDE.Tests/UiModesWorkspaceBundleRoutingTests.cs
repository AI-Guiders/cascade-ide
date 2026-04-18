using CascadeIDE.Features.UiChrome;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Репозиторный bundle <c>UiModes/workspace.toml</c> содержит все канонические intent-id (ADR 0051) —
/// иначе дефолты в коде и TOML разъедутся без явной ошибки.
/// </summary>
public sealed class UiModesWorkspaceBundleRoutingTests
{
    static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CascadeIDE.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("CascadeIDE.sln not found from test output path.");
    }

    [Fact]
    public void Bundle_workspace_toml_maps_all_AttentionRoutingIntentIds()
    {
        var path = Path.Combine(RepoRoot(), "UiModes", "workspace.toml");
        Assert.True(File.Exists(path), $"Missing {path}");

        var text = File.ReadAllText(path);
        var w = CascadeTomlSerializer.Deserialize<UiWorkspaceToml>(text);
        Assert.NotNull(w?.Routing?.Attention);

        foreach (var intent in new[]
                 {
                     AttentionRoutingIntentIds.SolutionExplorer,
                     AttentionRoutingIntentIds.Chat,
                     AttentionRoutingIntentIds.Git,
                     AttentionRoutingIntentIds.Terminal,
                     AttentionRoutingIntentIds.Editor,
                 })
        {
            Assert.True(
                w!.Routing!.Attention!.ContainsKey(intent),
                $"UiModes/workspace.toml [routing.attention] should define intent '{intent}'.");
        }
    }
}
