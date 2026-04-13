using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Примеры из <c>samples/MarkdownExamples/</c> — include в fenced-блоках; копируются в output тестов.
/// </summary>
public sealed class MarkdownExamplesVerificationTests
{
    private static string SampleMdPath =>
        Path.Combine(AppContext.BaseDirectory, "samples", "MarkdownExamples", "sample.md");

    [Fact]
    public void SampleFilesExistNextToTestOutput()
    {
        Assert.True(File.Exists(SampleMdPath), $"Expected {SampleMdPath}");
        var dir = Path.GetDirectoryName(SampleMdPath)!;
        Assert.True(File.Exists(Path.Combine(dir, "hello.mmd")));
        Assert.True(File.Exists(Path.Combine(dir, "hello.puml")));
    }

    [Fact]
    public void MarkdownIncludeExpansion_ReplacesIncludesInsideMermaidAndPlantUmlFences()
    {
        var md = File.ReadAllText(SampleMdPath);
        var expanded = MarkdownIncludeExpansion.ExpandMarkdown(md, SampleMdPath);

        Assert.DoesNotContain("{{ INCLUDE:", expanded, StringComparison.Ordinal);
        Assert.Contains("Hello", expanded, StringComparison.Ordinal);
        Assert.Contains("World", expanded, StringComparison.Ordinal);
        Assert.Contains("@startuml", expanded, StringComparison.Ordinal);
        Assert.Contains("Alice", expanded, StringComparison.Ordinal);
        Assert.Contains("graph TD", expanded, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AfterInclude_KrokiDisabled_LeavesDiagramFencesAsSource()
    {
        var md = File.ReadAllText(SampleMdPath);
        var expanded = MarkdownIncludeExpansion.ExpandMarkdown(md, SampleMdPath);
        var settings = new CascadeIdeSettings { MarkdownDiagrams = new MarkdownDiagramSettings { KrokiEnabled = false } };
        var after = await MarkdownDiagramExpansion.ExpandAsync(expanded, settings);

        Assert.Contains("```mermaid", after, StringComparison.Ordinal);
        Assert.Contains("```plantuml", after, StringComparison.Ordinal);
        Assert.Contains("graph LR", after, StringComparison.Ordinal);
    }
}
