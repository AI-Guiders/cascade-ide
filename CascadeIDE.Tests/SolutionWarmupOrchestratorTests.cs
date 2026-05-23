#nullable enable

using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.SolutionWarmup.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SolutionWarmupOrchestratorTests
{
    [Fact]
    public void OnSolutionScopeChanged_CancelsPreviousRun()
    {
        var bus = new InMemoryDataBus(asynchronousDispatch: false);
        var events = new List<SolutionWarmupLifecycle>();
        using var sub = bus.Subscribe<SolutionWarmupStateChanged>(e => events.Add(e.Lifecycle));

        var gate = new ManualResetEventSlim(false);
        var host = new SolutionWarmupHostCallbacks
        {
            GetWarmupSettings = () => new SolutionWarmupSettings { Enabled = true },
            GetActiveCsFilePath = () =>
            {
                gate.Wait(TimeSpan.FromSeconds(5));
                return null;
            },
        };

        using var orchestrator = new SolutionWarmupOrchestrator(bus, host);
        orchestrator.OnSolutionScopeChanged(@"C:\ws1", @"C:\ws1\a.sln");
        orchestrator.OnSolutionScopeChanged(@"C:\ws2", @"C:\ws2\b.sln");
        gate.Set();

        Assert.Contains(SolutionWarmupLifecycle.Cancelled, events);
        Assert.Contains(SolutionWarmupLifecycle.Running, events);
    }

    [Fact]
    public void WarmIndex_BuildsBracketCache()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cide-warmup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "Sample.cs");
        try
        {
            File.WriteAllText(
                file,
                """
                public class Sample
                {
                    public void Run() { }
                }
                """);

            Features.Chat.BracketMemberCompletionProvider.WarmIndex(file, dir);
            var matches = Features.Chat.BracketMemberCompletionProvider.GetMatches(file, dir, "R", 10);
            Assert.Contains(matches, m => m.Name == "Run");
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }
}
