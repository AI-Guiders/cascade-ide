#nullable enable
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Cdp;
using CascadeIDE.Intercom;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Operator → host Composer request latch (HOST lane). Journals locally + mirrors IdeShare;
/// CDT inject consumer is a later residual — share shelf is the share-3.8 wire now.
/// </summary>
internal static class GlassHostComposerRequest
{
    public const string Schema = "host_composer_request/v0";

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public sealed record Sent(string Id, string Body, string RoleLabel);

    /// <summary>Queue host Composer turn. Empty → null.</summary>
    public static Sent? TryEnqueue(string? raw, string? workspaceRoot = null, GlassIntercomChannel.Kind channel = GlassIntercomChannel.Kind.Radio)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var body = raw.Trim();
        if (GlassIntercomLane.IsComposerPlaceholder(body))
            return null;

        var id = Guid.NewGuid().ToString("N")[..12];
        var (name, kind) = LatchPaint.ResolveIntercomIdentity("pm", "human", null, null);
        var stamped = DateTimeOffset.UtcNow;
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
                lane = GlassIntercomLane.Code(GlassIntercomLane.Kind.Host),
                channel = channelCode,
                stamped_utc = stamped
            };
            var json = JsonSerializer.Serialize(doc, JsonOpts);
            var path = CdpHabitatPaths.HostComposerRequestLatchPath;
            var tmp = path + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);

            GlassIntercomJournal.Append(id, "pm", "host", body, "human", stamped, name, kind, channelCode);
            _ = GlassOperatorShareShelf.TryPut(
                body,
                workspaceRoot,
                id,
                GlassIntercomLane.ShareWhat(GlassIntercomLane.Kind.Host));
            return new Sent(id, body, LatchPaint.FormatIntercomRole("pm", "host", name, kind));
        }
        catch
        {
            return null;
        }
    }
}
