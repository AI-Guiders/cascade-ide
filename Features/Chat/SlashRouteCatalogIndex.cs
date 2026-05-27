#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Индекс slash-маршрутов из <see cref="IntentSlashCatalog"/> (ADR 0119, 0150).</summary>
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

    public static SlashArgTailKind GetArgTailKind(string slashPath)
    {
        var key = IntentSlashCatalog.NormalizeSlashPath(slashPath);
        if (SlashRouteCatalogPathsGenerated.TryGetArgTailKind(key, out var generated))
            return (SlashArgTailKind)generated;

        return Lazy.Value.ArgTailKind.TryGetValue(key, out var kind) ? kind : SlashArgTailKind.None;
    }

    public static bool RouteRequiresArgTail(string slashPath) =>
        GetArgTailKind(slashPath) == SlashArgTailKind.Required;

    public static bool AcceptsOptionalArgTail(string slashPath) =>
        GetArgTailKind(slashPath) == SlashArgTailKind.Optional;

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
        var argTailKind = new Dictionary<string, SlashArgTailKind>(StringComparer.OrdinalIgnoreCase);
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
            argTailKind[path] = inferArgTailKind(path, byPath, paths);

        return new Snapshot(byPath, intercomVerbs, argTailKind);
    }

    /// <summary>Fallback, если в TOML нет <c>arg_tail</c> (ADR 0150: в bundled catalog — всегда явно).</summary>
    private static SlashArgTailKind inferArgTailKind(
        string path,
        IReadOnlyDictionary<string, SlashRouteEntry> byPath,
        IReadOnlyList<string> allPaths)
    {
        if (!byPath.TryGetValue(path, out var route))
            return SlashArgTailKind.None;

        if (route.ArgTailKindExplicit is { } explicitKind)
            return explicitKind;

        if (route.AutoRunOnCommit && !route.AutoRunRequiresArgs)
            return SlashArgTailKind.None;

        if (route.Completion != SlashCompletionKind.None)
            return SlashArgTailKind.Required;

        var prefix = path + " ";
        if (allPaths.Any(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return SlashArgTailKind.Required;

        if (path.EndsWith(" open", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("dialog", StringComparison.OrdinalIgnoreCase))
        {
            return SlashArgTailKind.Required;
        }

        if (!string.IsNullOrWhiteSpace(route.CommandId)
            && IdeCommandsArgs.TryGetArgs(route.CommandId, out var args)
            && args.Length > 0
            && args.All(static a => !a.Required))
        {
            return SlashArgTailKind.Optional;
        }

        return SlashArgTailKind.None;
    }

    private sealed record Snapshot(
        Dictionary<string, SlashRouteEntry> ByPath,
        HashSet<(string Group, string Verb)> IntercomInnerVerbs,
        Dictionary<string, SlashArgTailKind> ArgTailKind);
}
