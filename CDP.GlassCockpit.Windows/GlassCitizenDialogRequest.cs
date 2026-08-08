#nullable enable
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Cdp;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Operator → citizen dialog request for habitat bridge (not guest PF Intercom latch).
/// Does not publish human→PF voice (would wake guest unread). Journals locally + request latch + IdeShare.
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
    public static Sent? TryEnqueue(string? raw, string? modelId = null, string? workspaceRoot = null, GlassIntercomChannel.Kind channel = GlassIntercomChannel.Kind.Radio) =>
        TryPublish(raw, modelId, workspaceRoot, channel, journal: true, mirrorShare: true);

    /// <summary>
    /// SoftFL densify: @Sierra/@citizen mention wake — request latch only (parity GlassIntercomSend.TryNotifyPf).
    /// Lane/journal letter already exists; this only wakes Completions bridge.
    /// </summary>
    public static Sent? TryNotifyCitizen(
        string? raw,
        string? modelId = null,
        string? workspaceRoot = null,
        GlassIntercomChannel.Kind channel = GlassIntercomChannel.Kind.Radio) =>
        TryPublish(raw, modelId, workspaceRoot, channel, journal: false, mirrorShare: false);

    static Sent? TryPublish(
        string? raw,
        string? modelId,
        string? workspaceRoot,
        GlassIntercomChannel.Kind channel,
        bool journal,
        bool mirrorShare)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var body = raw.Trim();
        if (GlassIntercomLane.IsComposerPlaceholder(body))
            return null;

        var id = Guid.NewGuid().ToString("N")[..12];
        var (name, kind) = LatchPaint.ResolveIntercomIdentity("pm", "human", null, null);
        var stamped = DateTimeOffset.UtcNow;
        var mid = string.IsNullOrWhiteSpace(modelId) ? null : modelId.Trim();
        var channelCode = GlassIntercomChannel.Code(channel);

        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var doc = new
            {
                schema = Schema,
                id,
                body,
                status = "pending",
                lane = GlassIntercomLane.Code(GlassIntercomLane.Kind.Cit),
                channel = channelCode,
                model_id = mid,
                stamped_utc = stamped
            };
            var json = JsonSerializer.Serialize(doc, JsonOpts);
            var path = CdpHabitatPaths.CitizenDialogRequestLatchPath;
            var tmp = path + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);

            if (journal)
                GlassIntercomJournal.Append(id, "pm", "cit", body, "human", stamped, name, kind, channelCode);
            if (mirrorShare)
            {
                _ = GlassOperatorShareShelf.TryPut(
                    body,
                    workspaceRoot,
                    id,
                    GlassIntercomLane.ShareWhat(GlassIntercomLane.Kind.Cit));
            }

            return new Sent(id, body, LatchPaint.FormatIntercomRole("pm", "cit", name, kind));
        }
        catch
        {
            return null;
        }
    }
}
