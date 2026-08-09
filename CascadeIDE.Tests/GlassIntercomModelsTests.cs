#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomModelsTests
{
    [Fact]
    public void SealedDirectory_has_priced_face_catalog()
    {
        var d = GlassIntercomModels.SealedDirectory;
        Assert.Equal(6, d.Count);
        Assert.Equal(GlassIntercomModels.KimiK26Id, d[0].Id);
        Assert.Equal("Kimi-K2.6 · 144/595", d[0].Line);
        Assert.Contains("144/595", d[0].PriceLine, StringComparison.Ordinal);
        Assert.Equal(GlassIntercomModels.Glm51Id, d[1].Id);
        Assert.Equal("GLM-5.1 · 163/680", d[1].Line);
        Assert.Equal(GlassIntercomModels.QwenCoderNextId, d[2].Id);
        Assert.Equal("Qwen3-Coder-Next · 100/200", d[2].Line);
        Assert.Equal(GlassIntercomModels.DeepSeekV4ProId, d[3].Id);
        Assert.Equal(GlassIntercomModels.Qwen36_35BId, d[4].Id);
        Assert.Equal(GlassIntercomModels.Qwen35_397BId, d[5].Id);
        Assert.Equal("Qwen3.5-397B-A17B · 750/890", d[5].Line);
    }

    [Fact]
    public void BuildDirectory_merges_cfg_sticky_and_extras()
    {
        var d = GlassIntercomModels.BuildDirectory(
            stickyModelId: "acme/custom-model",
            cfgModelId: GlassIntercomModels.KimiK26Id,
            extraIds: [GlassIntercomModels.Glm51Id, "vendor/extra"]);
        Assert.Equal(8, d.Count);
        Assert.NotNull(GlassIntercomModels.Find(d, "acme/custom-model"));
        Assert.Equal("custom-model", GlassIntercomModels.Find(d, "acme/custom-model")!.Value.Display);
        Assert.Equal("extra", GlassIntercomModels.Find(d, "vendor/extra")!.Value.Line);
        Assert.Equal("price · —", GlassIntercomModels.Find(d, "vendor/extra")!.Value.PriceLine);
    }

    [Fact]
    public void ResolveSelectedId_prefers_sticky_then_cfg()
    {
        var d = GlassIntercomModels.BuildDirectory(null, GlassIntercomModels.KimiK26Id);
        Assert.Equal(
            GlassIntercomModels.KimiK26Id,
            GlassIntercomModels.ResolveSelectedId(d, null, GlassIntercomModels.KimiK26Id));
        Assert.Equal(
            GlassIntercomModels.Glm51Id,
            GlassIntercomModels.ResolveSelectedId(
                d, GlassIntercomModels.Glm51Id, GlassIntercomModels.KimiK26Id));
    }

    [Fact]
    public void ToLatchModelId_default_clears()
    {
        Assert.Null(GlassIntercomModels.ToLatchModelId("default"));
        Assert.Equal(GlassIntercomModels.Glm51Id, GlassIntercomModels.ToLatchModelId(GlassIntercomModels.Glm51Id));
    }

    [Fact]
    public void FormatStatusLine_includes_cfg_and_price()
    {
        var e = GlassIntercomModels.SealedDirectory[0];
        var line = GlassIntercomModels.FormatStatusLine(e, wroteCfg: true);
        Assert.StartsWith("glass · model · Kimi-K2.6 · CFG · 144/595", line, StringComparison.Ordinal);
        Assert.EndsWith("/M", line, StringComparison.Ordinal);
    }
}
