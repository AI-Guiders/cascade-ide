using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CascadeIDE.Features.WebAiPortal.Application;

/// <summary>Сбор JSON для моста: fenced <c>json-cascade</c> или голый объект с <c>command_id</c> (копипаст из UI чата без backtick).</summary>
public static class WebAiPortalBridgePayloadResolution
{
    /// <summary>Вытащить <c>command_id</c> из уже разрешённой JSON-тела execute (до merge).</summary>
    public static bool TryGetCommandId(string jsonPayload, [NotNullWhen(true)] out string? commandId)
    {
        commandId = null;
        if (string.IsNullOrWhiteSpace(jsonPayload))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(jsonPayload);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("command_id", out var cid)
                || cid.ValueKind != JsonValueKind.String)
                return false;
            var s = cid.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return false;
            commandId = s.Trim();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryResolvePayload(string raw, [NotNullWhen(true)] out string? json, out PayloadSourceHint source)
    {
        json = null;
        source = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        raw = raw.Trim();
        if (WebAiPortalJsonCascadeFence.TryExtractFirst(raw, out var fenced))
        {
            json = fenced;
            source = PayloadSourceHint.FencedMarkdown;
            return true;
        }

        if (TryParseBareExecuteCommand(raw, out var bare))
        {
            json = bare;
            source = PayloadSourceHint.BareJson;
            return true;
        }

        return false;
    }

    public static bool TryParseBareExecuteCommand(string trimmedText, [NotNullWhen(true)] out string? json)
    {
        json = null;
        var t = trimmedText.Trim();
        if (t.Length < 12 || !t.StartsWith('{') || !t.Contains("\"command_id\"", StringComparison.Ordinal))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(t);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            if (!doc.RootElement.TryGetProperty("command_id", out var cidEl) ||
                cidEl.ValueKind != JsonValueKind.String)
                return false;
            if (string.IsNullOrWhiteSpace(cidEl.GetString()))
                return false;
            json = t;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public enum PayloadSourceHint
    {
        FencedMarkdown,
        BareJson,
        DomProbe,
    }
}
