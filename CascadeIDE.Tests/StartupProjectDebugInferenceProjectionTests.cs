#nullable enable
using System.Collections.ObjectModel;
using System.IO;
using CascadeIDE.Features.Launch.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class StartupProjectDebugInferenceProjectionTests
{
    [Fact]
    public void Empty_roots_yields_null()
    {
        var roots = new ObservableCollection<SolutionItem>();
        Assert.Null(StartupProjectDebugInferenceProjection.TryInferCanonicalCsproj(roots, null, null));
    }

    [Fact]
    public void Single_csproj_in_tree_is_picked()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cide-startup-infer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var csproj = Path.Combine(dir, "Only.csproj");
        File.WriteAllText(csproj, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        var roots = new ObservableCollection<SolutionItem>
        {
            SolutionItem.CreateProject("Only", csproj)
        };

        Assert.Equal(CanonicalFilePath.Normalize(csproj), StartupProjectDebugInferenceProjection.TryInferCanonicalCsproj(roots, null, null));
    }

    [Fact]
    public void Persisted_startup_false_when_missing_path()
    {
        Assert.False(StartupProjectDebugInferenceProjection.HasPersistedStartupPointingToExistingFile(null));
        Assert.False(StartupProjectDebugInferenceProjection.HasPersistedStartupPointingToExistingFile(""));
    }

    [Fact]
    public void Persisted_startup_true_when_file_exists()
    {
        var f = Path.GetTempFileName() + ".csproj";
        File.WriteAllText(f, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        try
        {
            Assert.True(StartupProjectDebugInferenceProjection.HasPersistedStartupPointingToExistingFile(f));
        }
        finally
        {
            try { File.Delete(f); } catch { /* ignore */ }
        }
    }
}
