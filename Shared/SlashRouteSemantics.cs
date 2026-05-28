#nullable enable

namespace CascadeIDE.Services;

/// <summary>Семантика slash path: domain · object · intent (ADR 0154).</summary>
public readonly record struct SlashSemanticFields(
    string Domain,
    string Object,
    string Intent,
    SlashPathRole PathRole = SlashPathRole.Canonical)
{
    public bool DomainOmittedInPath =>
        PathRole == SlashPathRole.Alias && !string.IsNullOrEmpty(Domain);
}

public enum SlashPathRole
{
    Canonical,
    Alias,
}

/// <summary>Вывод и проверка domain/object/intent для <c>[[command.form.slash]]</c>.</summary>
public static class SlashRouteSemantics
{
    private static readonly HashSet<string> SolutionElisionObjects =
        new(StringComparer.OrdinalIgnoreCase) { "build", "test", "debug", "format" };

    public static SlashSemanticFields Resolve(string slashPath, string? mapLevel = null)
    {
        var segs = SplitPath(slashPath);
        if (segs.Count == 0)
            return new("", "", "", SlashPathRole.Canonical);

        if (segs.Count == 1)
            return new(segs[0], "", "", SlashPathRole.Canonical);

        if (segs.Count == 2 && SolutionElisionObjects.Contains(segs[0]))
            return new("solution", segs[0], segs[1], SlashPathRole.Alias);

        if (segs[0].Equals("anchor", StringComparison.OrdinalIgnoreCase))
            return new("intercom", "anchor", segs[1], SlashPathRole.Alias);

        return segs[0].ToLowerInvariant() switch
        {
            "intercom" => resolveIntercom(segs),
            "solution" => resolveSolution(segs),
            "editor" => resolveEditor(segs),
            "map" => resolveMap(segs, mapLevel),
            "git" or "agent" or "chat" or "diagnostics" or "state" or "search" or "export" or "open"
                or "folder" or "portal" or "cockpit" or "help" or "file" or "pfd" or "related" or "mfd"
                or "preview" or "output" or "repository" or "instrumentation" or "terminal" or "problems"
                or "events" or "workspace" or "settings" or "readiness" or "index" or "tests" or "ide"
                => resolveDomainIntent(segs),
            _ => segs.Count >= 3
                ? new(segs[0], segs[1], joinTail(segs, 2), SlashPathRole.Canonical)
                : new(segs[0], "", segs[1], SlashPathRole.Canonical),
        };
    }

    public static string BuildCanonicalPath(string domain, string obj, string intent)
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrEmpty(domain))
            parts.Add(domain);
        if (!string.IsNullOrEmpty(obj))
            parts.Add(obj);
        if (!string.IsNullOrEmpty(intent))
            parts.Add(intent);

        return parts.Count == 0 ? "" : "/" + string.Join(' ', parts);
    }

    public static bool PathMatchesSemantic(string slashPath, SlashSemanticFields fields)
    {
        var path = normalizePath(slashPath);
        var canonical = BuildCanonicalPath(fields.Domain, fields.Object, fields.Intent);
        if (path.Equals(canonical, StringComparison.OrdinalIgnoreCase))
            return true;

        var segs = SplitPath(path);
        var required = collectRequiredTokens(fields);
        if (required.Count == 0)
            return segs.Count <= 1;

        foreach (var token in required)
        {
            if (tokenPresentInPath(token, segs))
                continue;

            if (fields.PathRole == SlashPathRole.Alias
                && token.Equals("project", StringComparison.OrdinalIgnoreCase)
                && fields.Intent.Equals("new", StringComparison.OrdinalIgnoreCase)
                && segs.Any(s => s.Equals("new", StringComparison.OrdinalIgnoreCase)))
                continue;

            if (fields.PathRole == SlashPathRole.Alias
                && fields.DomainOmittedInPath
                && token.Equals(fields.Domain, StringComparison.OrdinalIgnoreCase))
                continue;

            if (fields.PathRole == SlashPathRole.Alias
                && fields.Intent.Equals("set", StringComparison.OrdinalIgnoreCase)
                && fields.Object.Equals("type", StringComparison.OrdinalIgnoreCase)
                && token.Equals("set", StringComparison.OrdinalIgnoreCase))
                continue;

            return false;
        }

        return true;
    }

    private static bool tokenPresentInPath(string token, IReadOnlyList<string> segs)
    {
        if (segs.Any(s => s.Equals(token, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (!token.Contains('_', StringComparison.Ordinal))
            return false;

        var parts = token.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var idx = 0;
        foreach (var part in parts)
        {
            var found = false;
            for (; idx < segs.Count; idx++)
            {
                if (!segs[idx].Equals(part, StringComparison.OrdinalIgnoreCase))
                    continue;

                found = true;
                idx++;
                break;
            }

            if (!found)
                return false;
        }

        return true;
    }

    private static List<string> collectRequiredTokens(SlashSemanticFields fields)
    {
        var required = new List<string>(3);
        if (!fields.DomainOmittedInPath && !string.IsNullOrEmpty(fields.Domain))
            required.Add(fields.Domain);
        if (!string.IsNullOrEmpty(fields.Object))
            required.Add(fields.Object);
        if (!string.IsNullOrEmpty(fields.Intent))
            required.Add(fields.Intent);
        return required;
    }

    /// <summary>Подпись шага иерархии для popup (домен → объект → действие → аргумент).</summary>
    public static string GetNextStepLabel(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        SlashSemanticFields fields,
        string slashPath)
    {
        var idx = getSemanticStepIndex(tokens, endsWithSpace, fields, slashPath);
        return idx switch
        {
            0 => "домен",
            1 => "объект",
            2 => "действие",
            _ => "аргумент",
        };
    }

    public static string BuildSemanticBreadcrumb(
        IReadOnlyList<string> tokens,
        SlashSemanticFields fields,
        string slashPath)
    {
        var parts = new List<string> { "/" };
        if (fields.DomainOmittedInPath && !string.IsNullOrEmpty(fields.Domain))
            parts.Add($"({fields.Domain})");

        if (!string.IsNullOrEmpty(fields.Domain) && !fields.DomainOmittedInPath)
            parts.Add(fields.Domain);

        foreach (var t in tokens)
            parts.Add(t);

        if (tokens.Count == 0 && string.IsNullOrEmpty(fields.Domain))
            return $"/ → {GetNextStepLabel(tokens, endsWithSpace: false, fields, slashPath)}";

        parts.Add("…");
        return string.Join(" › ", parts);
    }

    private static int getSemanticStepIndex(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        SlashSemanticFields fields,
        string slashPath)
    {
        var idx = endsWithSpace ? tokens.Count : Math.Max(0, tokens.Count - 1);
        if (fields.DomainOmittedInPath || isDomainOmittedAlias(fields, slashPath))
            idx++;

        return idx;
    }

    private static bool isDomainOmittedAlias(SlashSemanticFields fields, string slashPath)
    {
        if (fields.PathRole != SlashPathRole.Alias)
            return false;

        var canonical = BuildCanonicalPath(fields.Domain, fields.Object, fields.Intent);
        return !slashPath.Equals(canonical, StringComparison.OrdinalIgnoreCase);
    }

    private static SlashSemanticFields resolveIntercom(IReadOnlyList<string> segs)
    {
        if (segs.Count == 2)
        {
            var second = segs[1];
            if (second.Equals("overview", StringComparison.OrdinalIgnoreCase)
                || second.Equals("show", StringComparison.OrdinalIgnoreCase))
                return new("intercom", "", second, SlashPathRole.Canonical);

            return new("intercom", "", second, SlashPathRole.Canonical);
        }

        if (segs.Count == 3)
            return new("intercom", segs[1], segs[2], SlashPathRole.Canonical);

        if (segs.Count >= 4)
            return new("intercom", segs[1], joinTail(segs, 2), SlashPathRole.Canonical);

        return new("intercom", segs[1], joinTail(segs, 2), SlashPathRole.Canonical);
    }

    private static SlashSemanticFields resolveSolution(IReadOnlyList<string> segs)
    {
        if (segs.Count == 2)
            return new("solution", "", segs[1], SlashPathRole.Canonical);

        if (segs.Count >= 3 && segs[1].Equals("new", StringComparison.OrdinalIgnoreCase))
            return new("solution", "project", "new", SlashPathRole.Alias);

        if (segs.Count >= 3 && segs[1].Equals("explorer", StringComparison.OrdinalIgnoreCase))
            return new("solution", "explorer", segs[2], SlashPathRole.Canonical);

        return new("solution", segs[1], joinTail(segs, 2), SlashPathRole.Canonical);
    }

    private static SlashSemanticFields resolveEditor(IReadOnlyList<string> segs)
    {
        if (segs.Count == 2)
            return new("editor", "", segs[1], SlashPathRole.Canonical);

        if (segs.Count == 3 && segs[1].Equals("line", StringComparison.OrdinalIgnoreCase))
            return new("editor", "line", segs[2], SlashPathRole.Canonical);

        if (segs.Count == 3 && (segs[1].Equals("select", StringComparison.OrdinalIgnoreCase)
                                || segs[1].Equals("reveal", StringComparison.OrdinalIgnoreCase)))
            return new("editor", "code", segs[1], SlashPathRole.Canonical);

        if (segs.Count == 3 && segs[1].Equals("layout", StringComparison.OrdinalIgnoreCase))
            return new("editor", "layout", segs[2], SlashPathRole.Canonical);

        return new("editor", segs[1], joinTail(segs, 2), SlashPathRole.Canonical);
    }

    private static SlashSemanticFields resolveMap(IReadOnlyList<string> segs, string? mapLevel)
    {
        if (segs.Count >= 3 && segs[1].Equals("type", StringComparison.OrdinalIgnoreCase))
            return new("map", "type", "set", SlashPathRole.Alias);

        if (segs.Count == 3 && segs[1].Equals("cycle", StringComparison.OrdinalIgnoreCase))
            return new("map", "cycle", segs[2], SlashPathRole.Canonical);

        return new("map", segs[1], joinTail(segs, 2), SlashPathRole.Canonical);
    }

    private static SlashSemanticFields resolveDomainIntent(IReadOnlyList<string> segs) =>
        segs.Count == 2
            ? new(segs[0], "", segs[1], SlashPathRole.Canonical)
            : new(segs[0], segs[1], joinTail(segs, 2), SlashPathRole.Canonical);

    private static string joinTail(IReadOnlyList<string> segs, int start)
    {
        if (start >= segs.Count)
            return "";

        return start == segs.Count - 1
            ? segs[start]
            : string.Join('_', segs.Skip(start));
    }

    private static List<string> SplitPath(string slashPath)
    {
        var path = normalizePath(slashPath);
        if (path.Length < 2)
            return [];

        return path[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static string normalizePath(string slashPath)
    {
        if (string.IsNullOrWhiteSpace(slashPath))
            return "";

        var t = slashPath.Trim();
        return t[0] == '/' ? t : "/" + t;
    }

}
