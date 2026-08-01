using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public class SoftOrganMfdGlanceTests
{
    [Theory]
    [InlineData("Build", "toolchain")]
    [InlineData("Terminal", "sys")]
    [InlineData("Problems", "review")]
    [InlineData("SemanticMap", "arch")]
    [InlineData("nope", null)]
    public void TryOrganIdForMfdPage_maps(string page, string? organ)
    {
        Assert.Equal(organ, SoftOrganMfdGlance.TryOrganIdForMfdPage(page));
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
        Assert.Contains("MSBuild host later", body);
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
        Assert.Contains("ConPTY later", body);
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
        Assert.Contains("Problems MFD", body);
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
        Assert.Contains("SemanticMap MFD", body);
    }
}
