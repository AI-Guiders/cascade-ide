#nullable enable
using System.Text;
using System.Text.Json;
using CascadeIDE.Features.Cdp;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// MFD instrument glance: SoftOrgan latch pulse → human body text (stub peel).
/// Build ← toolchain; Terminal ← sys (Glass WPF latch; CIDE Avalonia owns live hosts).
/// SolutionExplorer intentionally unbound: Glass .sln TreeView/glance is the instrument peel;
/// SoftOrganKind.FilesDesk is FM utility (CabinGlass pin only) — do not overlay FM latch.
/// RelatedFiles SoftOrganMfdGlance stays ←refactor; SoftOrganKind.FindDesk shares CabinGlass pin
/// but SoftOrganMfdGlance is 1:1 — do not displace refactor with find_desk (search ≠ debt/blast).
/// Live related host SSOT = Avalonia RelatedFilesMfdPageView — Glass stays latch glance until WPF peel.
/// SemanticMap SoftOrganMfdGlance ← arch SoftOrgan; live graph SSOT = Avalonia WorkspaceNavigationMapView
/// (Skia) — Glass stays latch glance until WPF peel (do not dump adjacency into TextBlock).
/// Problems SoftOrganMfdGlance ← review SoftOrgan; live list SSOT = Avalonia ProblemsMfdPageView
/// — Glass stays latch glance until WPF peel (sa_desk chrome ≠ Problems MFD).
/// Correspondence intentionally unbound: CabinGlass pin correspondence/crs → MFD only; no SoftOrganKind
/// (do not invent SoftOrgan; SoftOrganKind.Crm chrome stays await/callout — not CRS). Live CRS SSOT = Avalonia.
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
            "DebugStack" => "debug_desk",
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
            AppendIfInt(root, "bp_count", "bp", sb);
            AppendIfBool(root, "machine_ok", "machine_ok", sb);
            AppendIfBool(root, "stopped", "stopped", sb);
            AppendIfBool(root, "active_dap", "active_dap", sb);
            AppendIfString(root, "seat", "seat", sb);
            AppendIfString(root, "profile", "profile", sb);
            AppendIfString(root, "mode", "mode", sb);
            AppendIfString(root, "verdict", "verdict", sb);
            AppendIfString(root, "stamped_utc", "stamped", sb);

            AppendHostFootnote(title, sb);

            var body = sb.ToString().TrimEnd();
            return body.Length == 0 ? null : body;
        }
        catch
        {
            return null;
        }
    }

    static void AppendHostFootnote(string title, StringBuilder sb)
    {
        var line = title.ToLowerInvariant() switch
        {
            "sys" => "□ Glass peel · ■ Avalonia TerminalMfdPageView · ConPTY",
            "toolchain" => "□ Glass peel · ■ Avalonia BuildMfdPageView",
            "test_desk" => "□ Glass peel · ■ Avalonia TestsMfdPageView",
            "debug_desk" => "□ Glass peel · ■ Avalonia DebugStackMfdPageView",
            "review" => "□ Glass peel · ■ Avalonia ProblemsMfdPageView",
            "arch" => "□ Glass peel · ■ Avalonia WorkspaceNavigationMapView",
            "mcp" => "□ Glass peel · ■ AiChatSettings · mcp SoftOrgan",
            "report" => "□ Glass peel · ■ Avalonia MarkdownPreview",
            "refactor" => "□ Glass peel · ■ Avalonia RelatedFilesMfdPageView",
            _ => null
        };
        if (line is null)
            return;
        sb.AppendLine().AppendLine(line);
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
