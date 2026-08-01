#nullable enable
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Dual-seat Intercom partner presence latch (observability) — not voice.
/// Wire matches cdp-mcp <c>CideIntercomPresenceLatch</c>.
/// </summary>
internal static class GlassIntercomPresence
{
    public const string Schema = "cide_intercom_presence_latch/v0";
    public const string ViewerSeat = "pm"; // Glass = Who@PM

    public const int DefaultComposingTtlSeconds = 20;
    public const int DefaultBusyTtlSeconds = 120;

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>Publish this seat's presence (merge dual map). Returns false on bad state/IO.</summary>
    public static bool TryPublish(string seat, string state, int? ttlSeconds = null)
    {
        seat = seat.Trim().ToLowerInvariant();
        if (seat is not ("pf" or "pm"))
            return false;

        state = state.Trim().ToLowerInvariant() switch
        {
            "idle" or "clear" or "ready" => "idle",
            "composing" or "typing" or "draft" => "composing",
            "busy" or "working" or "generating" or "tools" => "busy",
            _ => ""
        };
        if (state.Length == 0)
            return false;

        var ttl = ttlSeconds ?? state switch
        {
            "composing" => DefaultComposingTtlSeconds,
            "busy" => DefaultBusyTtlSeconds,
            _ => 0
        };

        try
        {
            CdpHabitatPaths.EnsureStateRoot();
            var doc = TryReadRaw() ?? new PresenceDoc { Schema = Schema };
            doc.Schema = Schema;

            var now = DateTimeOffset.UtcNow;
            var existing = seat == "pm" ? doc.Pm : doc.Pf;
            if (existing is not null
                && string.Equals(existing.State, state, StringComparison.OrdinalIgnoreCase)
                && (now - existing.StampedUtc).TotalSeconds < 2)
                return true;

            var slot = new PresenceSeat
            {
                State = state,
                StampedUtc = now,
                TtlSeconds = ttl > 0 ? ttl : null
            };
            if (seat == "pm")
                doc.Pm = slot;
            else
                doc.Pf = slot;

            var path = CdpHabitatPaths.IntercomPresenceLatchPath;
            var tmp = path + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(doc, JsonOpts));
            File.Move(tmp, path, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Partner line for Glass (viewer=PM → @PF · state). Null when idle/missing.</summary>
    public static string? TryPartnerLine(string? json = null)
    {
        try
        {
            PresenceDoc? doc;
            if (json is not null)
                doc = JsonSerializer.Deserialize<PresenceDoc>(json, ReadOpts);
            else
                doc = TryReadRaw();

            if (doc is null || !string.Equals(doc.Schema, Schema, StringComparison.OrdinalIgnoreCase))
                return null;

            var now = DateTimeOffset.UtcNow;
            var partner = Effective(doc.Pf, now); // Glass watches PF
            if (partner is null || string.IsNullOrWhiteSpace(partner.State))
                return null;
            if (string.Equals(partner.State, "idle", StringComparison.OrdinalIgnoreCase))
                return null;

            return $"@PF · {partner.State}";
        }
        catch
        {
            return null;
        }
    }

    static PresenceDoc? TryReadRaw()
    {
        var path = CdpHabitatPaths.IntercomPresenceLatchPath;
        if (!File.Exists(path))
            return null;
        return JsonSerializer.Deserialize<PresenceDoc>(File.ReadAllText(path), ReadOpts);
    }

    static PresenceSeat? Effective(PresenceSeat? seat, DateTimeOffset now)
    {
        if (seat is null)
            return null;
        if (string.Equals(seat.State, "idle", StringComparison.OrdinalIgnoreCase))
            return seat;

        var ttl = seat.TtlSeconds ?? seat.State switch
        {
            "composing" => DefaultComposingTtlSeconds,
            "busy" => DefaultBusyTtlSeconds,
            _ => 0
        };
        if (ttl > 0 && (now - seat.StampedUtc).TotalSeconds > ttl)
            return new PresenceSeat { State = "stale", StampedUtc = seat.StampedUtc, TtlSeconds = seat.TtlSeconds };

        return seat;
    }

    sealed class PresenceDoc
    {
        public string Schema { get; set; } = GlassIntercomPresence.Schema;
        public PresenceSeat? Pf { get; set; }
        public PresenceSeat? Pm { get; set; }
    }

    sealed class PresenceSeat
    {
        public string State { get; set; } = "idle";
        public DateTimeOffset StampedUtc { get; set; }
        public int? TtlSeconds { get; set; }
    }
}
