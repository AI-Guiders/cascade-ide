#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomModelsTests
{
    [Fact]
    public void SealedDirectory_has_default_glm_qwen()
    {
        var d = GlassIntercomModels.SealedDirectory;
        Assert.Equal(3, d.Count);
        Assert.Equal("default", d[0].Id);
        Assert.Equal("default · CFG", d[0].Line);
        Assert.Equal(GlassIntercomModels.Glm51Id, d[1].Id);
        Assert.Equal("GLM-5.1", d[1].Line);
        Assert.Equal(GlassIntercomModels.QwenCoderNextId, d[2].Id);
        Assert.Equal("Qwen3-Coder-Next", d[2].Line);
    }

    [Fact]
    public void BuildDirectory_merges_sticky_and_extras()
    {
        var d = GlassIntercomModels.BuildDirectory(
            "acme/custom-model",
            ["zai-org/GLM-5.1", "vendor/extra"]);
        Assert.Equal(5, d.Count);
        Assert.NotNull(GlassIntercomModels.Find(d, "acme/custom-model"));
        Assert.Equal("custom-model", GlassIntercomModels.Find(d, "acme/custom-model")!.Value.Display);
        Assert.Equal("extra", GlassIntercomModels.Find(d, "vendor/extra")!.Value.Line);
    }

    [Fact]
    public void ResolveSelectedId_null_sticky_is_default()
    {
        var d = GlassIntercomModels.BuildDirectory(null);
        Assert.Equal(GlassIntercomModels.DefaultId, GlassIntercomModels.ResolveSelectedId(d, null));
        Assert.Equal(GlassIntercomModels.Glm51Id, GlassIntercomModels.ResolveSelectedId(d, GlassIntercomModels.Glm51Id));
    }

    [Fact]
    public void ToLatchModelId_default_clears()
    {
        Assert.Null(GlassIntercomModels.ToLatchModelId("default"));
        Assert.Equal(GlassIntercomModels.Glm51Id, GlassIntercomModels.ToLatchModelId(GlassIntercomModels.Glm51Id));
    }
}
