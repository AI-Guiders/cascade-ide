#nullable enable
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Operator (PM) → agent (PF) voice latch — same wire as CIDE <c>CdpIntercomVoicePublisher</c>.
/// </summary>
internal static partial class GlassIntercomSend
{
    public const string Schema = "cide_intercom_voice_latch/v0";

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public sealed record Sent(string Id, string Body, string RoleLabel);

    /// <summary>
    /// Publish human→PF. Body may include leading <c>@PF</c> (stripped). Empty → null.
    /// </summary>
    public static Sent? TrySend(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var text = raw.Trim();
        if (text.Equals("Message @PF…", StringComparison.Ordinal)
            || text.Equals("Message @PM…", StringComparison.Ordinal))
            return null;

        var body = PfLeading().Replace(text, "").Trim();
        if (body.Length == 0)
            body = text;

        var id = Guid.NewGuid().ToString("N")[..12];
        var doc = new IntercomVoiceDoc
        {
            Schema = Schema,
            Id = id,
            FromSeat = "pm",
            ToSeat = "pf",
            Body = body,
            Origin = "human",
            StampedUtc = DateTimeOffset.UtcNow,
            Acked = false
        };

        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var json = JsonSerializer.Serialize(doc, JsonOpts);
            var path = CdpHabitatPaths.IntercomLatchPath;
            var tmp = path + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);
            return new Sent(id, body, "@PM → @PF · human");
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(@"^@PF\b\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PfLeading();

    sealed class IntercomVoiceDoc
    {
        public string Schema { get; set; } = GlassIntercomSend.Schema;
        public string Id { get; set; } = "";
        public string FromSeat { get; set; } = "pm";
        public string ToSeat { get; set; } = "pf";
        public string Body { get; set; } = "";
        public string Origin { get; set; } = "human";
        public DateTimeOffset StampedUtc { get; set; }
        public bool Acked { get; set; }
    }
}
