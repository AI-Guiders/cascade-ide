using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace CascadeIDE.Models;

/// <summary>API-ключи для облачных провайдеров (не хранятся в settings.toml).</summary>
public sealed class AiKeys : ModelBase
{
    public string? AnthropicApiKey { get; set; }
    public string? OpenAiApiKey { get; set; }
    public string? DeepSeekApiKey { get; set; }

    public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
    {
        if (modelBase is not AiKeys o)
            return false;
        return AnthropicApiKey.Is(o.AnthropicApiKey)
            && OpenAiApiKey.Is(o.OpenAiApiKey)
            && DeepSeekApiKey.Is(o.DeepSeekApiKey);
    }

    public override ModelBase Clone()
    {
        return new AiKeys
        {
            AnthropicApiKey = AnthropicApiKey,
            OpenAiApiKey = OpenAiApiKey,
            DeepSeekApiKey = DeepSeekApiKey
        };
    }
}
