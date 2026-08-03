#nullable enable
using System.Text.Json;

namespace CascadeIDE.Intercom;

/// <summary>
/// Paint citizen-dialog-request latch status for Glass StatusText (habitat bridge progress).
/// </summary>
public static class CitizenDialogRequestStatus
{
    public sealed record View(string Id, string Status, string? Error, string? Peer, string StatusLine);

    public static View? TryPaint(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var id = Prop(root, "id") ?? "?";
            var status = (Prop(root, "status") ?? "pending").Trim().ToLowerInvariant();
            var error = Prop(root, "error");
            var peer = Prop(root, "peer");
            return new View(id, status, error, peer, FormatLine(id, status, error, peer));
        }
        catch
        {
            return null;
        }
    }

    public static string FormatLine(string id, string status, string? error, string? peer = null)
    {
        var shortId = id.Length > 8 ? id[..8] : id;
        return status switch
        {
            "pending" => $"glass · citizen · queued {shortId} · waiting habitat bridge",
            "running" => $"glass · citizen · {shortId} · running",
            "done" => string.IsNullOrWhiteSpace(peer)
                ? $"glass · citizen · {shortId} · done"
                : $"glass · citizen · {shortId} · done · {peer}",
            "error" => string.IsNullOrWhiteSpace(error)
                ? $"glass · citizen · {shortId} · error"
                : $"glass · citizen · {shortId} · error · {error}",
            _ => $"glass · citizen · {shortId} · {status}"
        };
    }

    static string? Prop(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
}
