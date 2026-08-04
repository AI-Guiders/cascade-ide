#nullable enable
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

    public static bool Claim(string seat, string name, string? kind = null)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            return false;
        var kindNorm = string.IsNullOrWhiteSpace(kind)
            ? (string.Equals(seat, "pm", StringComparison.OrdinalIgnoreCase) ? "operator" : "guest")
            : kind.Trim().ToLowerInvariant();

        lock (Gate)
        {
            try
            {
                CdpHabitatPaths.EnsureStateRoot();
                var doc = TryReadUnlocked() ?? new IdentityDoc { Schema = Schema };
                doc.Schema = Schema;
                var slot = new IdentitySeat
                {
                    Name = trimmed,
                    Kind = kindNorm,
                    StampedUtc = DateTimeOffset.UtcNow
                };
                if (string.Equals(seat, "pm", StringComparison.OrdinalIgnoreCase))
                    doc.Pm = slot;
                else
                    doc.Pf = slot;

                var json = JsonSerializer.Serialize(doc, JsonOpts);
                var tmp = LatchPath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
                File.WriteAllText(tmp, json);
                File.Move(tmp, LatchPath, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
        }
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
    }

    sealed class IdentitySeat
    {
        public string Name { get; set; } = "";
        public string? Kind { get; set; }
        public DateTimeOffset StampedUtc { get; set; }
    }
}
