#nullable enable

using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassCitizenAiKeysTests
{
    [Fact]
    public void TryWriteOpenAiModel_patches_without_wiping_other_keys()
    {
        var path = Path.Combine(Path.GetTempPath(), "glass-ai-keys-" + Guid.NewGuid().ToString("N") + ".toml");
        try
        {
            File.WriteAllText(path,
                """
                # keep me
                open_ai_api_key = "secret-key"
                open_ai_base_url = "https://example.test/v1"
                open_ai_model = "zai-org/GLM-5.1"
                anthropic_api_key = "anth"
                """.Replace("\r\n", "\n"));

            Assert.True(GlassCitizenAiKeys.TryWriteOpenAiModel(
                GlassIntercomModels.KimiK26Id, out var err, path));
            Assert.Null(err);

            var text = File.ReadAllText(path);
            Assert.Contains("open_ai_api_key = \"secret-key\"", text, StringComparison.Ordinal);
            Assert.Contains("open_ai_base_url = \"https://example.test/v1\"", text, StringComparison.Ordinal);
            Assert.Contains("anthropic_api_key = \"anth\"", text, StringComparison.Ordinal);
            Assert.Contains(
                $"open_ai_model = \"{GlassIntercomModels.KimiK26Id}\"",
                text,
                StringComparison.Ordinal);
            Assert.DoesNotContain("zai-org/GLM-5.1", text, StringComparison.Ordinal);
            Assert.Equal(GlassIntercomModels.KimiK26Id, GlassCitizenAiKeys.TryReadOpenAiModel(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryWriteOpenAiModel_appends_when_missing()
    {
        var path = Path.Combine(Path.GetTempPath(), "glass-ai-keys-" + Guid.NewGuid().ToString("N") + ".toml");
        try
        {
            File.WriteAllText(path, "open_ai_api_key = \"k\"\n");
            Assert.True(GlassCitizenAiKeys.TryWriteOpenAiModel("Qwen/Qwen3-Coder-Next", out _, path));
            var text = File.ReadAllText(path);
            Assert.Contains("open_ai_api_key = \"k\"", text, StringComparison.Ordinal);
            Assert.Contains("open_ai_model = \"Qwen/Qwen3-Coder-Next\"", text, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
