#nullable enable
using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassGlanceCardsTests
{
    [Fact]
    public void BuildEvents_ready_includes_latch_count()
    {
        var chips = GlassGlanceCards.BuildEvents(new GlassEventsGlance.EventsPresenceStatus(
            LatchLatestCount: 12,
            LatchRoot: "state",
            Catalog: ["BuildStateChanged"]));

        Assert.Equal(new GlassGlanceChip("LEVEL", "READY", "ok"), chips[0]);
        Assert.Contains(new GlassGlanceChip("LATCHES", "12", "ok"), chips);
    }

    [Fact]
    public void BuildWorkspaceHealth_projects_git_and_solution()
    {
        var chips = GlassGlanceCards.BuildWorkspaceHealth(new GlassWorkspaceHealthGlance.WorkspaceFsStatus(
            RootPath: @"D:\ws",
            RootExists: true,
            HasGit: true,
            SlnPath: @"D:\ws\CascadeIDE.sln",
            HasCascadeIdeDir: true));

        Assert.Equal(new GlassGlanceChip("LEVEL", "READY", "ok"), chips[0]);
        Assert.Contains(new GlassGlanceChip("GIT", "yes", "ok"), chips);
        Assert.Contains(new GlassGlanceChip("SLN", "CascadeIDE.sln", "ok"), chips);
    }

    [Fact]
    public void BuildEnvironment_projects_probe_rows()
    {
        var chips = GlassGlanceCards.BuildEnvironment(new GlassEnvironmentReadinessGlance.EnvProbeStatus(
            new GlassEnvironmentReadinessGlance.EnvProbeRow("AGENT_NOTES_FILE", "unset", null),
            new GlassEnvironmentReadinessGlance.EnvProbeRow("NETCOREDBG_PATH", "missing", "dbg.exe"),
            new GlassEnvironmentReadinessGlance.EnvProbeRow("dotnet", "ok", "dotnet.exe")));

        Assert.Equal(new GlassGlanceChip("LEVEL", "DEGRADED", "warn"), chips[0]);
        Assert.Contains(new GlassGlanceChip("NETCOREDBG_PATH", "missing · dbg.exe", "bad"), chips);
    }

    [Fact]
    public void BuildHypotheses_projects_status_counts()
    {
        var chips = GlassGlanceCards.BuildHypotheses(new GlassHypothesesGlance.HypothesesFsStatus(
            FilePath: @"D:\ws\.cascade-ide\debug-hypotheses.json",
            FileExists: true,
            Total: 3,
            Open: 1,
            Rejected: 1,
            Confirmed: 1,
            ModifiedUtc: null));

        Assert.Equal(new GlassGlanceChip("LEVEL", "READY", "ok"), chips[0]);
        Assert.Contains(new GlassGlanceChip("OPEN", "1", "warn"), chips);
        Assert.Contains(new GlassGlanceChip("CONFIRMED", "1", "ok"), chips);
    }

    [Fact]
    public void BuildFds_projects_shelf_presence()
    {
        var chips = GlassGlanceCards.BuildFds(new GlassGlanceCards.FdsShelfStatus(
            PlanReady: true,
            PlanPulse: "wave · shipping",
            SharedOn: false,
            SharedFile: null,
            ReportReady: true,
            ReportPulse: "report",
            PressureReady: true,
            PressureLine: "human-faced",
            WakeReady: true,
            WakeHint: "leaf wake",
            WorkspaceCdp: true));

        Assert.Equal(new GlassGlanceChip("LEVEL", "READY", "ok"), chips[0]);
        Assert.Contains(new GlassGlanceChip("PLAN", "wave · shipping", "ok"), chips);
        Assert.Contains(new GlassGlanceChip("PRESSURE", "human-faced", "warn"), chips);
        Assert.Contains(new GlassGlanceChip("WAKE", "leaf wake", "warn"), chips);
        Assert.Contains(new GlassGlanceChip(".CDP", "yes", "ok"), chips);
    }

    [Fact]
    public void BuildEditorSitu_projects_why_file_instrument_cards()
    {
        var face = new GlassEditorSituRibbon.Face(
            Why: "leaf · why-file",
            Blast: "A · B",
            BlastNames: ["A.cs", "B.cs"],
            RoleInGraph: "в карте",
            HopNodes: 3,
            HopEdges: 2,
            Orphan: false,
            LookMap: "карта → MFD",
            DiffIntent: "+2 −1",
            Diff: null,
            AppliesOnLocus: "E0 W1",
            Applies: null);

        var chips = GlassGlanceCards.BuildEditorSitu(face);

        Assert.Equal(new GlassGlanceChip("LEVEL", "SITU", "ok"), chips[0]);
        Assert.Contains(new GlassGlanceChip("WHY", "leaf · why-file", "ok"), chips);
        Assert.Contains(new GlassGlanceChip("BLAST", "A.cs · B.cs", "ok"), chips);
        Assert.Contains(new GlassGlanceChip("ROLE", "в карте", "ok"), chips);
        Assert.Contains(new GlassGlanceChip("HOPS", "3 узлов · 2 связей", "ok"), chips);
        Assert.Contains(new GlassGlanceChip("LOOK", "карта → MFD", "meta"), chips);
        Assert.Contains(new GlassGlanceChip("DIFF", "+2 −1", "warn"), chips);
        Assert.Contains(new GlassGlanceChip("APPLIES", "E0 W1", "ok"), chips);
    }

    [Fact]
    public void BuildChat_projects_presence_seats()
    {
        var chips = GlassGlanceCards.BuildChat(new GlassGlanceCards.ChatPresenceStatus("composing", "idle"));

        Assert.Equal(new GlassGlanceChip("LEVEL", "LIVE", "ok"), chips[0]);
        Assert.Contains(new GlassGlanceChip("@PF", "composing", "ok"), chips);
        Assert.Contains(new GlassGlanceChip("@PM", "idle", "idle"), chips);
    }

    [Fact]
    public void BuildChat_both_idle_is_idle_level()
    {
        var chips = GlassGlanceCards.BuildChat(new GlassGlanceCards.ChatPresenceStatus("idle", "idle"));
        Assert.Equal(new GlassGlanceChip("LEVEL", "IDLE", "idle"), chips[0]);
    }
}
