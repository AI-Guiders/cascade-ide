using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public class SoftOrganMfdGlanceTests
{
    [Theory]
    [InlineData("Build", "toolchain")]
    [InlineData("Tests", "test_desk")]
    [InlineData("DebugStack", "debug_desk")]
    [InlineData("Terminal", "sys")]
    [InlineData("Problems", "review")]
    [InlineData("SemanticMap", "arch")]
    [InlineData("AiChatSettings", "mcp")]
    [InlineData("MarkdownPreview", "report")]
    [InlineData("RelatedFiles", "refactor")]
    [InlineData("SolutionExplorer", null)]
    [InlineData("HybridIndex", null)]
    [InlineData("Correspondence", null)]
    [InlineData("nope", null)]
    public void TryOrganIdForMfdPage_maps(string page, string? organ)
    {
        Assert.Equal(organ, SoftOrganMfdGlance.TryOrganIdForMfdPage(page));
    }

    [Fact]
    public void TryFormatFromJson_test_desk_includes_counts_and_verdict()
    {
        const string json = """
            {
              "schema": "cide_test_desk_latch/v1",
              "active": true,
              "pulse": "test_desk · retest · FAIL 2/5",
              "verdict": "retest",
              "ok_count": 2,
              "total_count": 5,
              "failed": 3,
              "skipped": 0
            }
            """;

        var body = SoftOrganMfdGlance.TryFormatFromJson("test_desk", json);
        Assert.NotNull(body);
        Assert.Contains("test_desk latch glance · active", body);
        Assert.Contains("verdict=retest", body);
        Assert.Contains("ok=2", body);
        Assert.Contains("total=5", body);
        Assert.Contains("failed=3", body);
        Assert.Contains("TestsMfdPageView", body);
        Assert.Contains("□ Glass peel", body);
        Assert.Contains("┌ host", body);
    }

    [Fact]
    public void TryFormatFromJson_debug_desk_includes_bp_and_flags()
    {
        const string json = """
            {
              "schema": "cide_debug_desk_latch/v1",
              "active": true,
              "pulse": "debug_desk · continue · STOPPED t=1 · bp=2",
              "verdict": "continue",
              "bp_count": 2,
              "stopped": true,
              "active_dap": true
            }
            """;

        var body = SoftOrganMfdGlance.TryFormatFromJson("debug_desk", json);
        Assert.NotNull(body);
        Assert.Contains("debug_desk latch glance · active", body);
        Assert.Contains("verdict=continue", body);
        Assert.Contains("bp=2", body);
        Assert.Contains("stopped=true", body);
        Assert.Contains("active_dap=true", body);
        Assert.Contains("DebugStackMfdPageView", body);
        Assert.Contains("□ Glass peel", body);
    }

    [Fact]
    public void TryFormatFromJson_toolchain_includes_pulse_and_counts()
    {
        const string json = """
            {
              "schema": "cide_toolchain_latch/v1",
              "active": false,
              "pulse": "toolchain · 5/5 ok · go=toolchain",
              "ok_count": 5,
              "total_count": 5,
              "stamped_utc": "2026-07-31T13:52:14Z"
            }
            """;

        var body = SoftOrganMfdGlance.TryFormatFromJson("toolchain", json);
        Assert.NotNull(body);
        Assert.Contains("toolchain latch glance · idle", body);
        Assert.Contains("toolchain · 5/5 ok", body);
        Assert.Contains("ok=5", body);
        Assert.Contains("total=5", body);
        Assert.Contains("BuildMfdPageView", body);
        Assert.Contains("□ Glass peel", body);
    }

    [Fact]
    public void TryFormatFromJson_sys_notes_conpty_later()
    {
        const string json = """
            {
              "schema": "cide_sys_latch/v1",
              "active": true,
              "pulse": "ops · seat=cdp · clear",
              "seat": "cdp"
            }
            """;

        var body = SoftOrganMfdGlance.TryFormatFromJson("sys", json);
        Assert.NotNull(body);
        Assert.Contains("sys latch glance · active", body);
        Assert.Contains("ops · seat=cdp", body);
        Assert.Contains("seat=cdp", body);
        Assert.Contains("TerminalMfdPageView", body);
        Assert.Contains("ConPTY", body);
        Assert.Contains("□ Glass peel", body);
    }

    [Fact]
    public void TryFormatFromJson_review_includes_file_counts()
    {
        const string json = """
            {
              "schema": "cide_review_latch/v1",
              "active": true,
              "pulse": "review · ready ×28 · go=review",
              "file_count": 28,
              "high_risk": 0,
              "machine_ok": true
            }
            """;

        var body = SoftOrganMfdGlance.TryFormatFromJson("review", json);
        Assert.NotNull(body);
        Assert.Contains("review latch glance · active", body);
        Assert.Contains("files=28", body);
        Assert.Contains("high_risk=0", body);
        Assert.Contains("machine_ok=true", body);
        Assert.Contains("ProblemsMfdPageView", body);
        Assert.Contains("□ Glass peel", body);
    }

    [Fact]
    public void TryFormatFromJson_arch_includes_profile_mode()
    {
        const string json = """
            {
              "schema": "cide_arch_latch/v1",
              "active": true,
              "pulse": "as_built · cdp_desk · 10 roles",
              "profile": "cdp_desk",
              "mode": "as_built"
            }
            """;

        var body = SoftOrganMfdGlance.TryFormatFromJson("arch", json);
        Assert.NotNull(body);
        Assert.Contains("arch latch glance · active", body);
        Assert.Contains("profile=cdp_desk", body);
        Assert.Contains("mode=as_built", body);
        Assert.Contains("WorkspaceNavigationMapView", body);
        Assert.Contains("□ Glass peel", body);
    }

    [Fact]
    public void TryFormatFromJson_mcp_includes_mounted()
    {
        const string json = """
            {
              "schema": "cide_mcp_latch/v1",
              "active": false,
              "pulse": "mcp · idle",
              "mounted": 0
            }
            """;

        var body = SoftOrganMfdGlance.TryFormatFromJson("mcp", json);
        Assert.NotNull(body);
        Assert.Contains("mcp latch glance · idle", body);
        Assert.Contains("mounted=0", body);
        Assert.Contains("AiChatSettings", body);
    }

    [Fact]
    public void TryFormatFromJson_report_notes_md_host()
    {
        const string json = """
            {
              "schema": "cide_report_latch/v1",
              "active": false,
              "pulse": "report · idle"
            }
            """;

        var body = SoftOrganMfdGlance.TryFormatFromJson("report", json);
        Assert.NotNull(body);
        Assert.Contains("report latch glance · idle", body);
        Assert.Contains("MarkdownPreview", body);
        Assert.Contains("□ Glass peel", body);
    }

    [Fact]
    public void TryFormatFromJson_refactor_includes_hotspots()
    {
        const string json = """
            {
              "schema": "cide_refactor_latch/v1",
              "active": true,
              "pulse": "refactor · hotspots=3 · go=refactor",
              "hotspot_count": 3
            }
            """;

        var body = SoftOrganMfdGlance.TryFormatFromJson("refactor", json);
        Assert.NotNull(body);
        Assert.Contains("refactor latch glance · active", body);
        Assert.Contains("hotspots=3", body);
        Assert.Contains("RelatedFilesMfdPageView", body);
        Assert.Contains("□ Glass peel", body);
    }
}
