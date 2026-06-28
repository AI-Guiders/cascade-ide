using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Xunit;

using CascadeIDE.Features.Agent.Environment;

namespace CascadeIDE.Tests;

/// <summary>Канонические строки <c>presentation</c> и ожидаемое поведение (ADR 0017, 0168).</summary>
public static class PresentationTopologyCatalog
{
    public static PresentationGrammarTokens DefaultGrammar { get; } =
        PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");

    private static readonly string[] TripleLines =
    [
        "(P) (F) (M)",
        "(P) (M) (F)",
        "(F) (P) (M)",
        "(F) (M) (P)",
        "(M) (P) (F)",
        "(M) (F) (P)",
    ];

    public static IReadOnlyList<TopologyCase> All { get; } = BuildAll();

    private static IReadOnlyList<TopologyCase> BuildAll()
    {
        var list = new List<TopologyCase>
        {
            // --- один экран ---
            new(
                "(P+F+M)",
                MainScreen: 0,
                MfdHost: false,
                MfdHostScreen: null,
                PfdHost: false,
                PfdHostScreen: null,
                PmSplitHost: false,
                PmSplitScreen: null,
                MaximizeAtStartup: true,
                MainGridAtStartup: PresentationMainGridLayoutFrameBuilder.DefaultColumnDefinitions,
                PfdInMainAtStartup: true,
                MfdInMainAtStartup: true),

            new(
                "(0.2P+0.3F+0.5M)",
                MainScreen: 0,
                MfdHost: false,
                MfdHostScreen: null,
                PfdHost: false,
                PfdHostScreen: null,
                PmSplitHost: false,
                PmSplitScreen: null,
                MaximizeAtStartup: true,
                MainGridAtStartup: "0.2*,4,0.3*,4,0.5*",
                PfdInMainAtStartup: true,
                MfdInMainAtStartup: true),

            new(
                "(F)",
                MainScreen: 0,
                MfdHost: false,
                MfdHostScreen: null,
                PfdHost: false,
                PfdHostScreen: null,
                PmSplitHost: false,
                PmSplitScreen: null,
                MaximizeAtStartup: false,
                MainGridAtStartup: "0,4,*,4,0",
                PfdInMainAtStartup: false,
                MfdInMainAtStartup: false),

            // --- два экрана: F + M ---
            new(
                "(F) (M)",
                MainScreen: 0,
                MfdHost: true,
                MfdHostScreen: 1,
                PfdHost: false,
                PfdHostScreen: null,
                PmSplitHost: false,
                PmSplitScreen: null,
                MaximizeAtStartup: true,
                MainGridAtStartup: "0,4,*,4,0",
                PfdInMainAtStartup: false,
                MfdInMainAtStartup: false),

            new(
                "(M) (F)",
                MainScreen: 1,
                MfdHost: true,
                MfdHostScreen: 0,
                PfdHost: false,
                PfdHostScreen: null,
                PmSplitHost: false,
                PmSplitScreen: null,
                MaximizeAtStartup: true,
                MainGridAtStartup: "0,4,*,4,0",
                PfdInMainAtStartup: false,
                MfdInMainAtStartup: false),

            // --- два экрана: P+F + M ---
            new(
                "(P+F) (M)",
                MainScreen: 0,
                MfdHost: true,
                MfdHostScreen: 1,
                PfdHost: false,
                PfdHostScreen: null,
                PmSplitHost: false,
                PmSplitScreen: null,
                MaximizeAtStartup: true,
                MainGridAtStartup: "220,4,*,4,0",
                PfdInMainAtStartup: true,
                MfdInMainAtStartup: false),

            new(
                "(0.25P + 0.75F) (M)",
                MainScreen: 0,
                MfdHost: true,
                MfdHostScreen: 1,
                PfdHost: false,
                PfdHostScreen: null,
                PmSplitHost: false,
                PmSplitScreen: null,
                MaximizeAtStartup: true,
                MainGridAtStartup: "0.25*,4,0.75*,4,0",
                PfdInMainAtStartup: true,
                MfdInMainAtStartup: false),

            // --- два экрана: P+M + F ---
            new(
                "(P+M)(F)",
                MainScreen: 1,
                MfdHost: false,
                MfdHostScreen: null,
                PfdHost: false,
                PfdHostScreen: null,
                PmSplitHost: true,
                PmSplitScreen: 0,
                MaximizeAtStartup: true,
                MainGridAtStartup: "0,4,*,4,0",
                PfdInMainAtStartup: false,
                MfdInMainAtStartup: false),

            new(
                "(F)(P+M)",
                MainScreen: 0,
                MfdHost: false,
                MfdHostScreen: null,
                PfdHost: false,
                PfdHostScreen: null,
                PmSplitHost: true,
                PmSplitScreen: 1,
                MaximizeAtStartup: true,
                MainGridAtStartup: "0,4,*,4,0",
                PfdInMainAtStartup: false,
                MfdInMainAtStartup: false),

            new(
                "(0.25P + 0.75M)(F)",
                MainScreen: 1,
                MfdHost: false,
                MfdHostScreen: null,
                PfdHost: false,
                PfdHostScreen: null,
                PmSplitHost: true,
                PmSplitScreen: 0,
                MaximizeAtStartup: true,
                MainGridAtStartup: "0,4,*,4,0",
                PfdInMainAtStartup: false,
                MfdInMainAtStartup: false),
        };

        foreach (var line in TripleLines)
            list.Add(BuildTripleCase(line));

        return list;
    }

    private static TopologyCase BuildTripleCase(string line)
    {
        var parse = PresentationParser.Parse(line, DefaultGrammar);
        if (!parse.IsSuccess)
            throw new InvalidOperationException($"Catalog triple line failed to parse: {line}");

        static int IndexOf(PresentationParseResult parse, PresentationAnchorKind kind)
        {
            for (var i = 0; i < parse.Screens.Count; i++)
            {
                var screen = parse.Screens[i];
                if (screen.Count == 1 && screen[0].Kind == kind)
                    return i;
            }

            throw new InvalidOperationException($"Anchor {kind} not found in triple line.");
        }

        return new TopologyCase(
            line,
            MainScreen: IndexOf(parse, PresentationAnchorKind.Forward),
            MfdHost: true,
            MfdHostScreen: IndexOf(parse, PresentationAnchorKind.Mfd),
            PfdHost: true,
            PfdHostScreen: IndexOf(parse, PresentationAnchorKind.Pfd),
            PmSplitHost: false,
            PmSplitScreen: null,
            MaximizeAtStartup: true,
            MainGridAtStartup: "0,4,*,4,0",
            PfdInMainAtStartup: false,
            MfdInMainAtStartup: false);
    }

    public sealed record TopologyCase(
        string Line,
        int MainScreen,
        bool MfdHost,
        int? MfdHostScreen,
        bool PfdHost,
        int? PfdHostScreen,
        bool PmSplitHost,
        int? PmSplitScreen,
        bool MaximizeAtStartup,
        string MainGridAtStartup,
        bool PfdInMainAtStartup,
        bool MfdInMainAtStartup);
}

public sealed class PresentationTopologyMatrixTests
{
    public static IEnumerable<object[]> CatalogCases() =>
        PresentationTopologyCatalog.All.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(CatalogCases))]
    public void Parse_Succeeds(PresentationTopologyCatalog.TopologyCase expected)
    {
        var parse = PresentationParser.Parse(expected.Line, PresentationTopologyCatalog.DefaultGrammar);
        Assert.True(parse.IsSuccess, $"Failed to parse: {expected.Line}");
        Assert.True(parse.Screens.Count > 0);
    }

    [Theory]
    [MemberData(nameof(CatalogCases))]
    public void TopologyFlags_MatchExpectation(PresentationTopologyCatalog.TopologyCase expected)
    {
        var parse = PresentationParser.Parse(expected.Line, PresentationTopologyCatalog.DefaultGrammar);
        var flags = PresentationTopologyResolver.ResolveFlags(parse);

        Assert.Equal(expected.MfdHost, flags.MfdHostTopology);
        Assert.Equal(expected.PfdHost, flags.PfdHostTopology);
        Assert.Equal(expected.PmSplitHost, flags.PmHostTopology);
    }

    [Theory]
    [MemberData(nameof(CatalogCases))]
    public void MainWindowScreenIndex_Matches(PresentationTopologyCatalog.TopologyCase expected)
    {
        var parse = PresentationParser.Parse(expected.Line, PresentationTopologyCatalog.DefaultGrammar);
        Assert.Equal(
            expected.MainScreen,
            PresentationLayoutAnalyzer.GetMainWindowPresentationScreenIndexOrDefault(parse));
    }

    [Theory]
    [MemberData(nameof(CatalogCases))]
    public void HostScreenIndices_Match(PresentationTopologyCatalog.TopologyCase expected)
    {
        var parse = PresentationParser.Parse(expected.Line, PresentationTopologyCatalog.DefaultGrammar);

        if (expected.MfdHostScreen is int mfdIdx)
        {
            Assert.True(
                PresentationLayoutAnalyzer.TryGetMfdHostPresentationScreenIndex(parse.Screens, out var actual),
                expected.Line);
            Assert.Equal(mfdIdx, actual);
        }
        else
        {
            Assert.False(PresentationLayoutAnalyzer.TryGetMfdHostPresentationScreenIndex(parse.Screens, out _));
        }

        if (expected.PfdHostScreen is int pfdIdx)
        {
            Assert.True(
                PresentationLayoutAnalyzer.TryGetPfdHostPresentationScreenIndex(parse.Screens, out var actual),
                expected.Line);
            Assert.Equal(pfdIdx, actual);
        }
        else
        {
            Assert.False(PresentationLayoutAnalyzer.TryGetPfdHostPresentationScreenIndex(parse.Screens, out _));
        }

        if (expected.PmSplitScreen is int pmIdx)
        {
            Assert.True(
                PresentationLayoutAnalyzer.TryGetPmSplitHostPresentationScreenIndex(parse.Screens, out var actual),
                expected.Line);
            Assert.Equal(pmIdx, actual);
        }
        else
        {
            Assert.False(PresentationLayoutAnalyzer.TryGetPmSplitHostPresentationScreenIndex(parse.Screens, out _));
        }
    }

    [Theory]
    [MemberData(nameof(CatalogCases))]
    public void ShouldMaximizeMainWindowAtStartup_Matches(PresentationTopologyCatalog.TopologyCase expected)
    {
        var parse = PresentationParser.Parse(expected.Line, PresentationTopologyCatalog.DefaultGrammar);
        Assert.Equal(
            expected.MaximizeAtStartup,
            PresentationLayoutAnalyzer.ShouldMaximizeMainWindowAtStartup(parse.Screens));
    }

    [Theory]
    [MemberData(nameof(CatalogCases))]
    public void MainGridAtStartup_Matches(PresentationTopologyCatalog.TopologyCase expected)
    {
        var parse = PresentationParser.Parse(expected.Line, PresentationTopologyCatalog.DefaultGrammar);
        var flags = PresentationTopologyResolver.ResolveFlags(parse);
        var frame = PresentationTopologyResolver.BuildMainWindowGridAtStartup(parse, flags);

        Assert.Equal(expected.MainGridAtStartup, frame.ColumnDefinitions);
    }

    [Theory]
    [MemberData(nameof(CatalogCases))]
    public void ShellVisibilityAtStartup_Matches(PresentationTopologyCatalog.TopologyCase expected)
    {
        var parse = PresentationParser.Parse(expected.Line, PresentationTopologyCatalog.DefaultGrammar);
        var shell = MainWindowShellSurfaceCompositor.Compose(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: true,
                IntentChatPanelExpanded: true,
                SuppressPfdColumnForPfdHostWindow: expected.PfdHost,
                SuppressMfdColumnForMfdHostWindow: expected.MfdHost,
                ExpandedMfdWidthPixels: 340,
                CollapsedMfdWidthPixels: 8,
                DisplaySettings: new DisplaySettings(),
                SafetyLevel: AgentSafetyLevel.Confirm));

        Assert.Equal(expected.PfdInMainAtStartup, shell.PfdSurfaceVisible);
        Assert.Equal(expected.MfdInMainAtStartup, shell.MfdColumnVisibleInMainGrid);
    }

    [Theory]
    [MemberData(nameof(CatalogCases))]
    public void CockpitPolicy_RequiresZonesOnMainScreen(PresentationTopologyCatalog.TopologyCase expected)
    {
        var parse = PresentationParser.Parse(expected.Line, PresentationTopologyCatalog.DefaultGrammar);

        Assert.Equal(
            expected.PfdInMainAtStartup,
            CockpitPresentationLayoutPolicy.RequiresPfdRegionInMainWindow(parse));
        Assert.Equal(
            expected.MfdInMainAtStartup,
            CockpitPresentationLayoutPolicy.RequiresMfdRegionInMainWindow(parse));
    }

    [Fact]
    public void DedicatedPfM_Unweighted_MainGridHidesMfdTailWhenHostTopology()
    {
        var parse = PresentationParser.Parse("(P+F) (M)", PresentationTopologyCatalog.DefaultGrammar);
        var flags = PresentationTopologyResolver.ResolveFlags(parse);
        var frame = PresentationTopologyResolver.BuildMainWindowGridAtStartup(parse, flags);
        Assert.Equal("220,4,*,4,0", frame.ColumnDefinitions);
    }

    [Fact]
    public void ForwardOnlyMainScreen_NeverUsesDefaultThreeColumnGrid()
    {
        foreach (var c in PresentationTopologyCatalog.All.Where(x => x.MainGridAtStartup == "0,4,*,4,0"))
        {
            var parse = PresentationParser.Parse(c.Line, PresentationTopologyCatalog.DefaultGrammar);
            var flags = PresentationTopologyResolver.ResolveFlags(parse);
            var frame = PresentationTopologyResolver.BuildMainWindowGridAtStartup(parse, flags);

            Assert.NotEqual(PresentationMainGridLayoutFrameBuilder.DefaultColumnDefinitions, frame.ColumnDefinitions);
            Assert.DoesNotContain("220", frame.ColumnDefinitions);
            Assert.DoesNotContain("340", frame.ColumnDefinitions);
        }
    }
}
