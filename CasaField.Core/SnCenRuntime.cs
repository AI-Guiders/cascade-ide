namespace CasaField.Core;

public sealed record SnPorts(string Mode, IReadOnlyDictionary<string, double> Ports, bool CenOpen);

public sealed record CenTarget(
    string Kind,
    string ConceptId,
    string? DocPath,
    string? Section,
    string? File,
    int? Line,
    int? LineEnd);

public sealed record CenTask(
    string Intent,
    string Action,
    IReadOnlyList<string> Concepts,
    IReadOnlyList<CenTarget> Targets,
    double Confidence,
    string? QueryToDmn,
    IReadOnlyDictionary<string, double> TaskBand);

public sealed record HotPathResult(
    double WallMs,
    int FieldVersion,
    bool Stale,
    DecodedGrid Decoded,
    SnPorts Sn,
    CenTask Cen,
    IReadOnlyList<ClaimNavItem> ClaimsNav);

public static class SnCenRuntime
{
    public static SnPorts Route(DecodedGrid decoded, IReadOnlyList<ClaimNavItem> nav, double gateThreshold = 0.35)
    {
        var conf = decoded.ConceptIds.Count > 0 ? 1.0 : 0.2;
        var p2 = conf >= gateThreshold ? conf : conf * 0.5;
        var ports = new Dictionary<string, double>
        {
            ["p1_dmn_to_sn"] = 1.0,
            ["p2_sn_to_cen"] = p2,
            ["p3_cen_to_dmn"] = conf >= gateThreshold ? 0.0 : 0.25,
        };
        var mode = p2 >= gateThreshold ? "execute" : "probe";
        return new SnPorts(mode, ports, p2 >= gateThreshold);
    }

    public static CenTask Plan(
        string? query,
        DecodedGrid decoded,
        SnPorts sn,
        IReadOnlyList<ClaimNavItem> nav)
    {
        var words = Tokenize(query);
        var hits = new List<(int Score, ClaimNavItem Item)>();

        foreach (var item in nav)
        {
            var score = ScoreClaim(query ?? "", words, item);
            if (score > 0)
                hits.Add((score, item));
        }

        hits.Sort((a, b) => b.Score.CompareTo(a.Score));
        var top = hits.Take(5).Select(h => h.Item).ToList();
        if (top.Count == 0 && nav.Count > 0)
            top = nav.Take(3).ToList();

        var concepts = top.Select(t => t.ConceptId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var targets = BuildTargets(top);
        var action = targets.Any(t => t.Kind == "code") ? "open_code"
            : targets.Any(t => t.Kind == "kb") ? "open_kb"
            : "answer_from_memory";

        var conf = sn.Ports.GetValueOrDefault("p2_sn_to_cen");
        var taskBand = new Dictionary<string, double>
        {
            ["task.intent.query"] = string.IsNullOrEmpty(query) ? 0.0 : 1.0,
            ["task.action.open_kb"] = targets.Any(t => t.Kind == "kb") ? 1.0 : 0.0,
            ["task.action.open_code"] = targets.Any(t => t.Kind == "code") ? 1.0 : 0.0,
            ["task.confidence"] = conf,
        };

        return new CenTask(
            string.IsNullOrEmpty(query) ? "summarize" : "query",
            action,
            concepts,
            targets,
            conf,
            hits.Count == 0 && !string.IsNullOrEmpty(query) ? $"clarify:{query[..Math.Min(120, query.Length)]}" : null,
            taskBand);
    }

    private static List<string> Tokenize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];
        return query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 2)
            .Select(w => w.ToLowerInvariant())
            .ToList();
    }

    private static int ScoreClaim(string query, List<string> words, ClaimNavItem item)
    {
        var score = 0;
        var cid = item.ConceptId.ToLowerInvariant();
        var sec = item.Section.ToLowerInvariant();
        var q = query.ToLowerInvariant();
        if (cid.Contains(q, StringComparison.Ordinal) || q.Contains(cid, StringComparison.Ordinal))
            score += 12;
        foreach (var w in words)
        {
            if (cid.Contains(w, StringComparison.Ordinal)) score += 4;
            if (sec.Contains(w, StringComparison.Ordinal)) score += 3;
        }
        return score;
    }

    private static List<CenTarget> BuildTargets(IReadOnlyList<ClaimNavItem> items)
    {
        var targets = new List<CenTarget>();
        foreach (var item in items)
        {
            targets.Add(new CenTarget("kb", item.ConceptId, item.DocPath, item.Section, null, null, null));
            if (item.CodeAnchors is null)
                continue;
            foreach (var a in item.CodeAnchors)
            {
                targets.Add(new CenTarget("code", item.ConceptId, null, null, a.File, a.Line, a.LineEnd));
            }
        }
        return targets;
    }
}

public static class CasaFieldHotPath
{
    public static HotPathResult Run(string storeDirectory, string? query = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var fieldPath = Path.Combine(storeDirectory, "field_state.json");
        var snapshot = FieldStateLoader.Load(fieldPath, storeDirectory);
        if (snapshot.Grid is null)
            throw new InvalidOperationException("field_state has no grid");

        var decoded = GridDecoder.Decode(snapshot.Grid.Cells, CasaCanonical.FieldMinVotes);
        var sn = SnCenRuntime.Route(decoded, snapshot.ClaimsNav);
        var cen = SnCenRuntime.Plan(query, decoded, sn, snapshot.ClaimsNav);
        sw.Stop();
        return new HotPathResult(
            Math.Round(sw.Elapsed.TotalMilliseconds, 2),
            snapshot.FieldVersion,
            snapshot.Stale,
            decoded,
            sn,
            cen,
            snapshot.ClaimsNav);
    }
}
