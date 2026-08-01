#nullable enable
using System.Text;
using System.Text.Json;
using CascadeIDE.Features.Cdp;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// MFD instrument glance: SoftOrgan latch pulse → human body text (stub peel).
/// Build ← toolchain; Terminal ← sys (desk ops until ConPTY).
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
            AppendIfString(root, "seat", "seat", sb);
            AppendIfString(root, "stamped_utc", "stamped", sb);

            if (string.Equals(title, "sys", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine().AppendLine("(Terminal ConPTY later — sys desk ops until shell latch.)");
            else if (string.Equals(title, "toolchain", StringComparison.OrdinalIgnoreCase))
                sb.AppendLine().AppendLine("(Build MFD ← toolchain SoftOrgan; MSBuild host later.)");

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

    static void AppendIfString(JsonElement root, string prop, string label, StringBuilder sb)
    {
        if (root.TryGetProperty(prop, out var el)
            && el.ValueKind == JsonValueKind.String
            && el.GetString() is { Length: > 0 } s)
            sb.Append(label).Append('=').Append(s.Trim()).AppendLine();
    }
}
