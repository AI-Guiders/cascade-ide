#nullable enable

using System.Text.Json;

namespace CascadeIDE.Intercom;

/// <summary>
/// Intercom lane × model axes (design glass-intercom-lane-model-axes-v0).
/// Lane = CIT|HOST|PF Korry; model = FM id only when lane=CIT. Pure — no WPF.
/// </summary>
public static class GlassIntercomLane
{
    public const string Schema = "glass_intercom_lane/v0";

    public enum Kind
    {
        Cit,
        Host,
        Pf
    }

    public readonly record struct Snapshot(Kind Lane, string? ModelId);

    public static Kind DefaultLane => Kind.Pf;

    public static string Code(Kind lane) => lane switch
    {
        Kind.Cit => "cit",
        Kind.Host => "host",
        Kind.Pf => "pf",
        _ => "pf"
    };

    public static string Label(Kind lane) => lane switch
    {
        Kind.Cit => "CIT",
        Kind.Host => "HOST",
        Kind.Pf => "PF",
        _ => "PF"
    };

    public static string Tooltip(Kind lane) => lane switch
    {
        Kind.Cit => "Citizen · MAF / FM path",
        Kind.Host => "Composer · host",
        Kind.Pf => "PF · habitat partner",
        _ => "PF · habitat partner"
    };

    public static string ComposerHint(Kind lane) => lane switch
    {
        Kind.Cit => "Message @CIT…",
        Kind.Host => "Message @HOST…",
        _ => "Message @PF…"
    };

    public static bool ModelAxisLit(Kind lane) => lane == Kind.Cit;

    public static bool IsComposerPlaceholder(string? text) =>
        text is "Message @PF…" or "Message @PM…"
            or "Message @CIT…" or "Message @HOST…";

    public static Kind Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultLane;

        var t = raw.Trim().ToLowerInvariant();
        return t switch
        {
            "cit" or "citizen" => Kind.Cit,
            "host" or "composer" => Kind.Host,
            "pf" or "habitat" => Kind.Pf,
            _ => DefaultLane
        };
    }

    /// <summary>Migrate pre-axes ModelPicker labels (Citizen · default / Composer · host / PF · habitat).</summary>
    public static Kind FromLegacyModelChoice(string? modelChoice)
    {
        if (string.IsNullOrWhiteSpace(modelChoice))
            return DefaultLane;

        var t = modelChoice.Trim();
        if (t.Contains("Citizen", StringComparison.OrdinalIgnoreCase)
            || t.Contains("CIT", StringComparison.OrdinalIgnoreCase))
            return Kind.Cit;
        if (t.Contains("Composer", StringComparison.OrdinalIgnoreCase)
            || t.Contains("host", StringComparison.OrdinalIgnoreCase))
            return Kind.Host;
        if (t.Contains("PF", StringComparison.OrdinalIgnoreCase)
            || t.Contains("habitat", StringComparison.OrdinalIgnoreCase))
            return Kind.Pf;
        return DefaultLane;
    }

    public static Snapshot ParseLatchJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new Snapshot(DefaultLane, null);

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return new Snapshot(DefaultLane, null);

            Kind lane;
            if (root.TryGetProperty("lane", out var laneEl) && laneEl.ValueKind == JsonValueKind.String)
                lane = Parse(laneEl.GetString());
            else if (root.TryGetProperty("model", out var legacy) && legacy.ValueKind == JsonValueKind.String)
                lane = FromLegacyModelChoice(legacy.GetString());
            else
                lane = DefaultLane;

            string? modelId = null;
            if (root.TryGetProperty("model_id", out var mid) && mid.ValueKind == JsonValueKind.String)
                modelId = mid.GetString();
            else if (ModelAxisLit(lane)
                     && root.TryGetProperty("model", out var m)
                     && m.ValueKind == JsonValueKind.String)
            {
                var s = m.GetString();
                // Prefer model_id; ignore legacy lane labels stuffed in model=
                if (s is { Length: > 0 }
                    && !s.Contains('·')
                    && !s.Contains("Citizen", StringComparison.OrdinalIgnoreCase)
                    && !s.Contains("Composer", StringComparison.OrdinalIgnoreCase))
                    modelId = s;
            }

            return new Snapshot(lane, modelId);
        }
        catch
        {
            return new Snapshot(DefaultLane, null);
        }
    }

    public static string FormatLatchJson(Kind lane, string? modelId, DateTimeOffset? stampedUtc = null)
    {
        var stamp = (stampedUtc ?? DateTimeOffset.UtcNow).ToString("o");
        var mid = string.IsNullOrWhiteSpace(modelId) ? null : modelId.Trim();
        var doc = new Dictionary<string, object?>
        {
            ["schema"] = Schema,
            ["lane"] = Code(lane),
            ["model_id"] = mid,
            ["stamped_utc"] = stamp
        };
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }
}
