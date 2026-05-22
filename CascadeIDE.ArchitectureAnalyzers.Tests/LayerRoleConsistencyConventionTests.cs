#nullable enable

using Xunit;

namespace CascadeIDE.ArchitectureAnalyzers.Tests;

/// <summary>
/// Соглашения слоя Application / ComputingUnits в репозитории (без полной компиляции CascadeIDE).
/// </summary>
public sealed class LayerRoleConsistencyConventionTests
{
    private static string CascadeIdeRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Application_OrchestratorFiles_HaveApplicationOrchestratorAttribute()
    {
        var violations = new List<string>();
        foreach (var file in EnumerateApplicationCs("*Orchestrator.cs"))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("class ", StringComparison.Ordinal))
                continue;
            if (text.Contains("class ", StringComparison.Ordinal)
                && text.Contains("Orchestrator", StringComparison.Ordinal)
                && !text.Contains("[ApplicationOrchestrator", StringComparison.Ordinal))
            {
                violations.Add(Relative(file));
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Application_PresentationProjectionFiles_HaveRoleMarker()
    {
        var violations = new List<string>();
        foreach (var file in EnumerateApplicationCs("*PresentationProjection.cs"))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("[PresentationProjection", StringComparison.Ordinal)
                && !text.Contains("[ComputingUnit", StringComparison.Ordinal))
            {
                violations.Add(Relative(file));
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Application_ShortProjectionFiles_HaveRoleMarker()
    {
        var violations = new List<string>();
        foreach (var file in EnumerateApplicationCs("*Projection.cs"))
        {
            if (file.EndsWith("PresentationProjection.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var text = File.ReadAllText(file);
            if (!text.Contains("[PresentationProjection", StringComparison.Ordinal)
                && !text.Contains("[ComputingUnit", StringComparison.Ordinal))
            {
                violations.Add(Relative(file));
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void ComputingUnits_ImplementingCcu_HaveComputingUnitAttribute()
    {
        var violations = new List<string>();
        var cuDir = Path.Combine(CascadeIdeRoot, "Cockpit", "ComputingUnits");
        foreach (var file in Directory.EnumerateFiles(cuDir, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains(": ICockpitComputeUnit", StringComparison.Ordinal))
                continue;
            if (text.Contains(": ICockpitComputeUnitPayload", StringComparison.Ordinal))
                continue;
            if (!text.Contains("[ComputingUnit", StringComparison.Ordinal))
                violations.Add(Relative(file));
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Application_NoComputingUnitOnOrchestratorName()
    {
        var violations = new List<string>();
        foreach (var file in EnumerateApplicationCs("*Orchestrator.cs"))
        {
            var text = File.ReadAllText(file);
            if (text.Contains("[ComputingUnit", StringComparison.Ordinal)
                && text.Contains("Orchestrator", StringComparison.Ordinal))
            {
                violations.Add(Relative(file));
            }
        }

        Assert.Empty(violations);
    }

    private static IEnumerable<string> EnumerateApplicationCs(string fileNamePattern)
    {
        var features = Path.Combine(CascadeIdeRoot, "Features");
        if (!Directory.Exists(features))
            yield break;

        foreach (var file in Directory.EnumerateFiles(features, fileNamePattern, SearchOption.AllDirectories))
        {
            var normalized = file.Replace('\\', '/');
            if (normalized.Contains("/Application/", StringComparison.OrdinalIgnoreCase))
                yield return file;
        }
    }

    private static string Relative(string absolutePath) =>
        Path.GetRelativePath(CascadeIdeRoot, absolutePath);
}
