#nullable enable
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Cdp;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Operator → citizen dialog request for habitat bridge (not guest PF Intercom latch).
/// Does not publish human→PF voice (would wake guest unread). Journals locally + request latch.
/// </summary>
internal static class GlassCitizenDialogRequest
{
    public const string Schema = "citizen_dialog_request/v0";

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public sealed record Sent(string Id, string Body, string RoleLabel);

    /// <summary>Queue citizen dialog turn. Empty → null.</summary>
    public static Sent? TryEnqueue(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var body = raw.Trim();
        if (GlassIntercomLane.IsComposerPlaceholder(body))
            return null;

        var id = Guid.NewGuid().ToString("N")[..12];
        var (name, kind) = LatchPaint.ResolveIntercomIdentity("pm", "human", null, null);
        var stamped = DateTimeOffset.UtcNow;

        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var doc = new
            {
                schema = Schema,
                id,
                body,
                status = "pending",
                stamped_utc = stamped
            };
            var json = JsonSerializer.Serialize(doc, JsonOpts);
            var path = CdpHabitatPaths.CitizenDialogRequestLatchPath;
            var tmp = path + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);

            // Journal only — do not Publish intercom-LATEST (guest PF unread).
            GlassIntercomJournal.Append(id, "pm", "pf", body, "human", stamped, name, kind);
            return new Sent(id, body, LatchPaint.FormatIntercomRole("pm", "pf", name, kind));
        }
        catch
        {
            return null;
        }
    }
}
