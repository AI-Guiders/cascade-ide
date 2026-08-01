#nullable enable
using System.Text;
using System.Text.Json;
using CascadeIDE.Features.Cdp;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// MFD instrument glance: SoftOrgan latch pulse → human body text (stub peel).
/// Build ← toolchain; Terminal ← sys (Glass WPF latch; CIDE Avalonia owns live hosts).
/// </summary>
public static class SoftOrganMfdGlance
{
    /// <summary>Map Glass/CIDE MFD page name → SoftOrgan latch stem (null = no glance).</summary>
    public static string? TryOrganIdForMfdPage(string? mfdPage)
    {
        if (string.IsNullOrWhiteSpace(mfdPage))
            return null;

        return mfdPage.Trim() switch
        {
            "Build" => "toolchain",
            "Tests" => "test_desk",
            "Terminal" => "sys",
            "Problems" => "review",
            "SemanticMap" => "arch",
            "AiChatSettings" => "mcp",
            "MarkdownPreview" => "report",
            "RelatedFiles" => "refactor",
            _ => null
        };
    }

    /// <summary>Read latch file and format glance body (null if missing/unreadable).</summary>
    public static string? TryFormatFromOrganId(string organId)
    {
        var id = SoftOrganLatchCatalog.Canonicalize(organId);
        if (id.Length == 0 || !SoftOrganLatchCatalog.Contains(id))
            return null;

        var path = CdpHabitatPaths.GetLatchPath(id + "-LATEST.json");
        var raw = CdpLatchIo.TryReadAllTextIfExists(path);
        return raw is null ? null : TryFormatFromJson(id, raw);
    }

    /// <summary>Format SoftOrgan latch JSON into MFD body (testable; no I/O).</summary>
    public static string? TryFormatFromJson(string organId, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var sb = new StringBuilder();
            var title = SoftOrganLatchCatalog.Canonicalize(organId);
            sb.Append(title).Append(" latch glance");

            if (root.TryGetProperty("active", out var activeEl)
                && activeEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                sb.Append(activeEl.GetBoolean() ? " · active" : " · idle");

            sb.AppendLine();
            sb.AppendLine();

            string? pulseText = null;
            if (root.TryGetProperty("pulse", out var pulseEl)
                && pulseEl.ValueKind == JsonValueKind.String
                && pulseEl.GetString() is { Length: > 0 } pulse)
            {
                pulseText = pulse.Trim();
                sb.AppendLine(pulseText);
            }

            if (root.TryGetProperty("chrome_hint", out var hintEl)
                && hintEl.ValueKind == JsonValueKind.String
                && hintEl.GetString() is { Length: > 0 } hint)
            {
                var hintText = hint.Trim();
                if (!string.Equals(hintText, pulseText, StringComparison.Ordinal))
                    sb.AppendLine(hintText);
            }

            AppendIfInt(root, "ok_count", "ok", sb);
            AppendIfInt(root, "total_count", "total", sb);
            AppendIfInt(root, "failed", "failed", sb);
            AppendIfInt(root, "skipped", "skipped", sb);
            AppendIfInt(root, "file_count", "files", sb);
            AppendIfInt(root, "high_risk", "high_risk", sb);
            AppendIfInt(root, "mounted", "mounted", sb);
            AppendIfInt(root, "hotspot_count", "hotspots", sb);
            AppendIfBool(root, "machine_ok", "machine_ok", sb);
            AppendIfString(root, "seat", "seat", sb);
            AppendIfString(root, "profile", "profile", sb);
            AppendIfString(root, "mode", "mode", sb);
            AppendIfString(root, "verdict", "verdict", sb);
            AppendIfString(root, "stamped_utc", "stamped", sb);

            if (string.Equals(title, "sys", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine().AppendLine("(Terminal: CIDE Avalonia ConPTY = Views/TerminalMfdPageView; Glass WPF host deferred — latch glance only.)");
            else if (string.Equals(title, "toolchain", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine().AppendLine("(Build: CIDE Avalonia BuildMfdPageView + BuildOutputPanel; Glass WPF host deferred — latch glance only.)");
            else if (string.Equals(title, "test_desk", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine().AppendLine("(Tests MFD ← test_desk SoftOrgan; live host = CIDE Avalonia TestsMfdPageView; Glass WPF host deferred — latch glance only.)");
            else if (string.Equals(title, "review", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine().AppendLine("(Problems MFD ← review SoftOrgan; Roslyn Problems host later.)");
            else if (string.Equals(title, "arch", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine().AppendLine("(SemanticMap MFD ← arch SoftOrgan; graph host later.)");
            else if (string.Equals(title, "mcp", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine().AppendLine("(AiChatSettings MFD ← mcp SoftOrgan; mount panel later.)");
            else if (string.Equals(title, "report", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine().AppendLine("(MarkdownPreview MFD ← report SoftOrgan; md host later.)");
            else if (string.Equals(title, "refactor", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine().AppendLine("(RelatedFiles MFD ← refactor SoftOrgan; find_usages host later.)");

            var body = sb.ToString().TrimEnd();
            return body.Length == 0 ? null : body;
        }
        catch
        {
            return null;
        }
    }

    static void AppendIfInt(JsonElement root, string prop, string label, StringBuilder sb)
    {
        if (root.TryGetProperty(prop, out var el) && el.TryGetInt32(out var n))
            sb.Append(label).Append('=').Append(n).AppendLine();
    }

    static void AppendIfBool(JsonElement root, string prop, string label, StringBuilder sb)
    {
        if (root.TryGetProperty(prop, out var el)
            && (el.ValueKind is JsonValueKind.True or JsonValueKind.False))
            sb.Append(label).Append('=').Append(el.GetBoolean() ? "true" : "false").AppendLine();
    }

    static void AppendIfString(JsonElement root, string prop, string label, StringBuilder sb)
    {
        if (root.TryGetProperty(prop, out var el)
            && el.ValueKind == JsonValueKind.String
            && el.GetString() is { Length: > 0 } s)
            sb.Append(label).Append('=').Append(s.Trim()).AppendLine();
    }
}
