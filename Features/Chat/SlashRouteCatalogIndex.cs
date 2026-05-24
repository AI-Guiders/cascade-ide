#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Индекс slash-маршрутов из <see cref="IntentSlashCatalog"/> (ADR 0119).</summary>
internal static class SlashRouteCatalogIndex
{
    private static readonly Lazy<Snapshot> Lazy = new(static () => Build(IntentSlashCatalog.SlashRoutes));

    public static bool TryGetRoute(string slashPath, out SlashRouteEntry route) =>
        Lazy.Value.ByPath.TryGetValue(IntentSlashCatalog.NormalizeSlashPath(slashPath), out route);

    public static bool IsKnownIntercomInnerVerb(string group, string verb)
    {
        if (string.IsNullOrWhiteSpace(group) || string.IsNullOrWhiteSpace(verb))
            return false;

        return Lazy.Value.IntercomInnerVerbs.Contains((group.ToLowerInvariant(), verb.ToLowerInvariant()));
    }

    public static bool RouteRequiresArgTail(string slashPath)
    {
        var key = IntentSlashCatalog.NormalizeSlashPath(slashPath);
        return Lazy.Value.RequiresArgTail.TryGetValue(key, out var requires) && requires;
    }

    public static bool TryGetIntercomHandler(string slashPath, out string? handlerId)
    {
        handlerId = null;
        if (!TryGetRoute(slashPath, out var route))
            return false;

        handlerId = route.IntercomHandlerId;
        return !string.IsNullOrWhiteSpace(handlerId);
    }

    private static Snapshot Build(IReadOnlyDictionary<string, SlashRouteEntry> routes)
    {
        var byPath = new Dictionary<string, SlashRouteEntry>(routes, StringComparer.OrdinalIgnoreCase);
        var intercomVerbs = new HashSet<(string Group, string Verb)>();
        var requiresArgTail = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var paths = routes.Keys.OrderBy(static p => p.Length).ToList();

        foreach (var path in paths)
        {
            if (!path.StartsWith("/intercom ", StringComparison.OrdinalIgnoreCase))
                continue;

            var body = path["/intercom ".Length..];
            var space = body.IndexOf(' ');
            if (space < 0)
                continue;

            var group = body[..space];
            var rest = body[(space + 1)..].Trim();
            var verbSpace = rest.IndexOf(' ');
            var verb = verbSpace < 0 ? rest : rest[..verbSpace];
            if (verb.Length > 0)
                intercomVerbs.Add((group.ToLowerInvariant(), verb.ToLowerInvariant()));
        }

        foreach (var path in paths)
            requiresArgTail[path] = inferRequiresArgTail(path, byPath, paths);

        return new Snapshot(byPath, intercomVerbs, requiresArgTail);
    }

    private static bool inferRequiresArgTail(
        string path,
        IReadOnlyDictionary<string, SlashRouteEntry> byPath,
        IReadOnlyList<string> allPaths)
    {
        if (!byPath.TryGetValue(path, out var route))
            return false;

        if (route.AutoRunOnCommit && !route.AutoRunRequiresArgs)
            return false;

        if (route.RequiresArgTailExplicit is { } explicitFlag)
            return explicitFlag;

        if (route.Completion != SlashCompletionKind.None)
            return true;

        var prefix = path + " ";
        if (allPaths.Any(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (path.EndsWith(" open", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("dialog", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private sealed record Snapshot(
        Dictionary<string, SlashRouteEntry> ByPath,
        HashSet<(string Group, string Verb)> IntercomInnerVerbs,
        Dictionary<string, bool> RequiresArgTail);
}
