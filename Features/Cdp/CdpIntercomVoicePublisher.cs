#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CascadeIDE.Features.Cdp;

/// <summary>
/// Operator Intercom → agent PF latch.
/// When user message addresses @PF, publishes intercom-LATEST (origin=human).
/// Agent surfaces via <c>cdp_intercom</c> / cockpit pulse — not a parallel peek API.
/// </summary>
internal static partial class CdpIntercomVoicePublisher
{
    public const string Schema = "cide_intercom_voice_latch/v0";
    public const string OriginHuman = "human";
    public const string SeatPf = "pf";
    public const string SeatPm = "pm";

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string StateRoot => CdpHabitatPaths.StateRoot;

    public static string LatchPath => CdpHabitatPaths.IntercomLatchPath;

    /// <summary>True when body addresses @PF (word boundary).</summary>
    public static bool TryExtractPfBody(string? raw, out string body)
    {
        body = "";
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = raw.Trim();
        if (!PfMention().IsMatch(text))
            return false;

        var stripped = PfLeading().Replace(text, "").Trim();
        body = stripped.Length > 0 ? stripped : text;
        return true;
    }

    public static string? TryPublishFromOperator(string? rawDisplay)
    {
        if (!TryExtractPfBody(rawDisplay, out var body))
            return null;

        var id = Guid.NewGuid().ToString("N")[..12];
        var doc = new IntercomVoiceDoc
        {
            Schema = Schema,
            Id = id,
            FromSeat = SeatPm,
            ToSeat = SeatPf,
            Body = body,
            Origin = OriginHuman,
            StampedUtc = DateTimeOffset.UtcNow,
            Acked = false
        };

        try
        {
            Directory.CreateDirectory(StateRoot);
            var json = JsonSerializer.Serialize(doc, JsonOpts);
            var tmp = LatchPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, LatchPath, overwrite: true);
            CdpIntercomVoiceProjector.Instance?.SuppressEcho(id);
            return id;
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(@"@PF\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PfMention();

    [GeneratedRegex(@"^@PF\b\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PfLeading();

    sealed class IntercomVoiceDoc
    {
        public string Schema { get; set; } = CdpIntercomVoicePublisher.Schema;
        public string Id { get; set; } = "";
        public string FromSeat { get; set; } = SeatPm;
        public string ToSeat { get; set; } = SeatPf;
        public string Body { get; set; } = "";
        public string Origin { get; set; } = OriginHuman;
        public DateTimeOffset StampedUtc { get; set; }
        public bool Acked { get; set; }
    }
}
