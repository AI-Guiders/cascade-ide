#nullable enable
using System.ClientModel;
using Anthropic;
using Microsoft.Extensions.AI;

namespace CascadeIDE.Services;

internal static class CascadeIdeMafChatClientFactories
{
    /// <summary>База OpenAI-совместимого API: к SDK нужен суффикс <c>/v1</c> (совпадает с прежним Http-клиентом: base + <c>v1/chat/completions</c>).</summary>
    public static Uri NormalizeOpenAiCompatibleEndpoint(string baseUrl)
    {
        var t = (baseUrl ?? "").Trim().TrimEnd('/');
        if (t.Length == 0)
            t = "https://api.openai.com";
        if (!t.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            t += "/v1";
        return new Uri(t + "/", UriKind.Absolute);
    }

    public static Microsoft.Extensions.AI.IChatClient? CreateOpenAiCompatibleChatClientOrNull(string apiKey, string baseUrl, string modelId)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;
        var model = (modelId ?? "").Trim();
        if (model.Length == 0)
            return null;

        var cred = new ApiKeyCredential(apiKey.Trim());
        var opts = new OpenAI.OpenAIClientOptions { Endpoint = NormalizeOpenAiCompatibleEndpoint(baseUrl) };
        var root = new OpenAI.OpenAIClient(cred, opts);
        return root.GetChatClient(model).AsIChatClient();
    }

    public static Microsoft.Extensions.AI.IChatClient? CreateAnthropicChatClientOrNull(string apiKey, string modelId)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;
        var model = (modelId ?? "").Trim();
        if (model.Length == 0)
            return null;

        var native = new AnthropicClient { ApiKey = apiKey.Trim() };
        return native.AsIChatClient(model);
    }
}
