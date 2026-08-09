#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Sticky Intercom Who per seat — parity with cdp-mcp <c>CideIntercomIdentityLatch</c>.
/// </summary>
internal static class GlassIntercomIdentity
{
    public const string Schema = "cide_intercom_identity_latch/v0";
    public const string FileName = "intercom-identity-LATEST.json";

    static readonly object Gate = new();

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

    public static string LatchPath => Path.Combine(CdpHabitatPaths.StateRoot, FileName);

    public static (string? Name, string? Kind) TrySeat(string seat)
    {
        lock (Gate)
        {
            var doc = TryReadUnlocked();
            if (doc is null)
                return (null, null);
            var slot = string.Equals(seat, "pm", StringComparison.OrdinalIgnoreCase) ? doc.Pm : doc.Pf;
            if (slot is null || string.IsNullOrWhiteSpace(slot.Name))
                return (null, null);
            return (slot.Name.Trim(), slot.Kind);
        }
    }

    /// <summary>
    /// FM Face on seat (profiles) — tip plane ≠ Face after multi-principal.
    /// Prefers non-harness citizen profile for mention wake sink.
    /// </summary>
    public static (string? Name, string? Kind) TryCitizenFace(string seat)
    {
        lock (Gate)
        {
            var doc = TryReadUnlocked();
            if (doc is null)
                return (null, null);
            var profiles = string.Equals(seat, "pm", StringComparison.OrdinalIgnoreCase)
                ? doc.PmProfiles
                : doc.PfProfiles;
            if (profiles is null || profiles.Count == 0)
                return (null, null);

            IdentitySeat? best = null;
            foreach (var kv in profiles)
            {
                if (kv.Key.StartsWith("harness:", StringComparison.OrdinalIgnoreCase))
                    continue;
                var p = kv.Value;
                if (p is null || string.IsNullOrWhiteSpace(p.Name))
                    continue;
                var kind = (p.Kind ?? "").Trim().ToLowerInvariant();
                if (kind is not ("citizen" or "fm" or "peer"))
                    continue;
                if (best is null || p.StampedUtc > best.StampedUtc)
                    best = p;
            }

            if (best is null)
                return (null, null);
            return (best.Name.Trim(), string.IsNullOrWhiteSpace(best.Kind) ? "citizen" : best.Kind.Trim());
        }
    }

    /// <summary>
    /// Sealed — CDP <c>CideIntercomIdentityLatch.Claim</c> is the sole writer
    /// (harness:* vs FM model slots + citizen demote). Glass is read-only.
    /// </summary>
    [Obsolete("Identity SSOT write = cdp-mcp CideIntercomIdentityLatch.Claim; Glass read-only.")]
    public static bool Claim(string seat, string name, string? kind = null)
    {
        _ = seat;
        _ = name;
        _ = kind;
        return false;
    }

    static IdentityDoc? TryReadUnlocked()
    {
        try
        {
            if (!File.Exists(LatchPath))
                return null;
            var raw = File.ReadAllText(LatchPath);
            var doc = JsonSerializer.Deserialize<IdentityDoc>(raw, ReadOpts);
            if (doc is null || !string.Equals(doc.Schema, Schema, StringComparison.OrdinalIgnoreCase))
                return null;
            return doc;
        }
        catch
        {
            return null;
        }
    }

    sealed class IdentityDoc
    {
        public string Schema { get; set; } = GlassIntercomIdentity.Schema;
        public IdentitySeat? Pf { get; set; }
        public IdentitySeat? Pm { get; set; }
        public Dictionary<string, IdentitySeat>? PfProfiles { get; set; }
        public Dictionary<string, IdentitySeat>? PmProfiles { get; set; }
    }

    sealed class IdentitySeat
    {
        public string Name { get; set; } = "";
        public string? Kind { get; set; }
        public string? Model { get; set; }
        public DateTimeOffset StampedUtc { get; set; }
    }
}
