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

            // Compact metrics chip row (skip stamped — noise for humans).
            var chips = new List<string>();
            AppendChipInt(root, "ok_count", "ok", chips);
            AppendChipInt(root, "total_count", "total", chips);
            AppendChipInt(root, "failed", "failed", chips);
            AppendChipInt(root, "skipped", "skipped", chips);
            AppendChipInt(root, "file_count", "files", chips);
            AppendChipInt(root, "high_risk", "high_risk", chips);
            AppendChipInt(root, "mounted", "mounted", chips);
            AppendChipInt(root, "hotspot_count", "hotspots", chips);
            AppendChipInt(root, "bp_count", "bp", chips);
            AppendChipBool(root, "machine_ok", "machine_ok", chips);
            AppendChipBool(root, "stopped", "stopped", chips);
            AppendChipBool(root, "active_dap", "active_dap", chips);
            AppendChipString(root, "verdict", "verdict", chips);
            AppendChipString(root, "profile", "profile", chips);
            AppendChipString(root, "mode", "mode", chips);
            // seat only when not already in pulse
            if (root.TryGetProperty("seat", out var seatEl)
                && seatEl.ValueKind == JsonValueKind.String
                && seatEl.GetString() is { Length: > 0 } seat
                && (pulseText is null || pulseText.Contains("seat=", StringComparison.Ordinal) is false))
                chips.Add("seat=" + seat.Trim());

            if (chips.Count > 0)
                sb.AppendLine(string.Join(" · ", chips));

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
        if (title.Equals("sys", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("┌ host ───────────────┐");
            sb.AppendLine("│ ■ Glass redirected TextBox │");
            sb.AppendLine("│ □ Avalonia ConPTY SSOT │");
            sb.AppendLine("└──────────────────────┘");
            return;
        }

        if (title.Equals("toolchain", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("┌ host ───────────────┐");
            sb.AppendLine("│ ■ Glass redirected log TextBox │");
            sb.AppendLine("│ □ Avalonia BuildMfdPageView │");
            sb.AppendLine("└──────────────────────┘");
            return;
        }

        if (title.Equals("test_desk", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("┌ host ───────────────┐");
            sb.AppendLine("│ ■ Glass redirected log TextBox │");
            sb.AppendLine("│ □ Avalonia TestsMfdPageView │");
            sb.AppendLine("└──────────────────────┘");
            return;
        }

        var host = title.ToLowerInvariant() switch
        {
            "debug_desk" => ("■ Glass live latch ListBox", "□ Avalonia IdeDapDebugSession"),
            "review" => ("■ Glass Problems ListBox", "□ Avalonia ProblemsMfdPageView"),
            "arch" => ("■ Glass Semantic list v1", "□ Avalonia Skia map"),
            "mcp" => ("■ AiChatSettings SoftOrgan", "□ options host"),
            "report" => ("■ Glass Markdig FlowDocument", "□ Avalonia MarkdownPreview"),
            "refactor" => ("■ Glass RelatedFiles WNM-shape feed", "□ Avalonia RelatedFilesMfd / IdeMcp"),
            _ => ((string?)null, (string?)null)
        };
        if (host.Item1 is null)
            return;

        sb.AppendLine("┌ host ───────────────┐");
        sb.Append("│ ").Append(host.Item1).AppendLine(" │");
        sb.Append("│ ").Append(host.Item2).AppendLine(" │");
        sb.AppendLine("└──────────────────────┘");
    }



    static void AppendChipInt(JsonElement root, string prop, string label, List<string> chips)
    {
        if (root.TryGetProperty(prop, out var el) && el.TryGetInt32(out var n))
            chips.Add(label + "=" + n);
    }

    static void AppendChipBool(JsonElement root, string prop, string label, List<string> chips)
    {
        if (root.TryGetProperty(prop, out var el)
            && (el.ValueKind is JsonValueKind.True or JsonValueKind.False))
            chips.Add(label + "=" + (el.GetBoolean() ? "true" : "false"));
    }

    static void AppendChipString(JsonElement root, string prop, string label, List<string> chips)
    {
        if (root.TryGetProperty(prop, out var el)
            && el.ValueKind == JsonValueKind.String
            && el.GetString() is { Length: > 0 } s)
            chips.Add(label + "=" + s.Trim());
    }
}
