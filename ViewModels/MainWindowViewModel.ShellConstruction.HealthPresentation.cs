using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Cockpit.Channels.EnvironmentReadiness;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.Composition.EnvironmentReadiness;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.WorkspaceHealth;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.ViewModels;

/// <summary>Ctor health/presentation factory — returns values for readonly assign in the constructor.</summary>
public partial class MainWindowViewModel
{
    readonly record struct HealthPresentationBundle(
        IIdeHealthChannel WorkspaceHealth,
        IIdeHealthSurfaceCompositor WorkspaceHealthSurface,
        LatchEicasFeed EicasFeed,
        IEnvironmentReadinessChannel EnvironmentReadiness,
        IEnvironmentReadinessSurfaceCompositor EnvironmentReadinessSurface,
        PresentationParseResult PresentationParse,
        bool DedicatedMfdSecondScreen,
        bool TripleOneAnchorPerZone,
        bool MfdHostTopology,
        bool PmForwardTwoScreen,
        bool PmHostTopology,
        IInstrumentMountPolicyResolver InstrumentMountPolicy);

    HealthPresentationBundle CreateHealthAndPresentation()
    {
        var workspaceHealth = new IdeHealthSnapshotUnit(_ideDataBus);
        var workspaceHealthSurface = new IdeHealthSurfaceCompositor();
        var eicasFeed = new LatchEicasFeed();
        var environmentReadiness = new EnvironmentReadinessChannel();
        var environmentReadinessSurface = new EnvironmentReadinessSurfaceCompositor();

        var pg = _settings.GetEffectivePresentationGrammar();
        var grammar = PresentationGrammarTokens.FromSettings(
            pg.Brackets,
            pg.BetweenScreens,
            pg.BetweenZones,
            pg.Pfd,
            pg.Forward,
            pg.Mfd);
        var presentationParse = PresentationParser.Parse(_settings.GetEffectivePresentationLine(), grammar);
        var topologyFlags = PresentationTopologyResolver.ResolveFlags(presentationParse);

        return new HealthPresentationBundle(
            workspaceHealth,
            workspaceHealthSurface,
            eicasFeed,
            environmentReadiness,
            environmentReadinessSurface,
            presentationParse,
            topologyFlags.DedicatedMfdSecondScreen,
            topologyFlags.TripleOneAnchorPerZone,
            topologyFlags.MfdHostTopology,
            topologyFlags.PmForwardTwoScreen,
            topologyFlags.PmHostTopology,
            new SettingsBackedInstrumentMountPolicyResolver());
    }

    void WireHealthAndPresentationAfterConstruct()
    {
        SeedIdeHealthDataBus();
        Chrome.AfterGitWorkspaceHealthSummaryApplied = PublishGitToIdeDataBusAndRebuildIdeHealth;
        _eicasFeed.MessagesChanged += (_, _) => RebuildEicas();
        Workspace.PropertyChanged += (_, e) => OnWorkspacePropertyChanged(e.PropertyName);
        RebuildIdeHealth();
        RebuildEicas();
        SyncMfdShellPageForPrimaryWorkSurface();
        InitializePresentationTier();
        NotifyDockedInstrumentSlotBindings();
        EnsureAgentEnvironmentWiring();
    }
}
