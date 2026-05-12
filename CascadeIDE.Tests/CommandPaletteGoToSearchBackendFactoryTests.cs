using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Search.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CommandPaletteGoToSearchBackendFactoryTests
{
    [Fact]
    public void Resolve_WhenHybridDisabled_AlwaysRipgrep_ForAnyKind()
    {
        using var bus = new InMemoryDataBus(asynchronousDispatch: false);
        using var orchestrator = new HybridIndexOrchestrator(bus, ".hybrid-codebase-index");

        var hci = CommandPaletteGoToSearchBackendFactory.Resolve(
            CommandPaletteGoToSearchBackendKind.Hci,
            orchestrator,
            "workspace+solution",
            hybridIntegrationEnabled: false);

        var auto = CommandPaletteGoToSearchBackendFactory.Resolve(
            CommandPaletteGoToSearchBackendKind.Auto,
            orchestrator,
            "workspace+solution",
            hybridIntegrationEnabled: false);

        Assert.IsType<RipgrepCommandPaletteGoToSearchBackend>(hci);
        Assert.Same(hci, auto);
    }

    [Fact]
    public void Resolve_WhenHybridEnabled_Rg_ReturnsRipgrep()
    {
        using var bus = new InMemoryDataBus(asynchronousDispatch: false);
        using var orchestrator = new HybridIndexOrchestrator(bus, ".hybrid-codebase-index");

        var be = CommandPaletteGoToSearchBackendFactory.Resolve(
            CommandPaletteGoToSearchBackendKind.Rg,
            orchestrator,
            "workspace+solution",
            hybridIntegrationEnabled: true);

        Assert.IsType<RipgrepCommandPaletteGoToSearchBackend>(be);
    }

    [Fact]
    public void Resolve_WhenHybridEnabled_Hci_ReturnsHybridBackend()
    {
        using var bus = new InMemoryDataBus(asynchronousDispatch: false);
        using var orchestrator = new HybridIndexOrchestrator(bus, ".hybrid-codebase-index");

        var be = CommandPaletteGoToSearchBackendFactory.Resolve(
            CommandPaletteGoToSearchBackendKind.Hci,
            orchestrator,
            "workspace+solution",
            hybridIntegrationEnabled: true);

        Assert.IsType<HybridIndexCommandPaletteGoToSearchBackend>(be);
    }

    [Fact]
    public void Resolve_WhenHybridEnabled_Auto_ReturnsCompositeAuto()
    {
        using var bus = new InMemoryDataBus(asynchronousDispatch: false);
        using var orchestrator = new HybridIndexOrchestrator(bus, ".hybrid-codebase-index");

        var be = CommandPaletteGoToSearchBackendFactory.Resolve(
            CommandPaletteGoToSearchBackendKind.Auto,
            orchestrator,
            "workspace+solution",
            hybridIntegrationEnabled: true);

        Assert.IsType<CompositeAutoCommandPaletteGoToSearchBackend>(be);
    }
}
