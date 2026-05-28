#nullable enable
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Индекс slash по domain → object → intent (ADR 0154) для popup autocomplete.</summary>
internal static class SlashSemanticCatalogIndex
{
    private static readonly Lazy<Snapshot> Lazy = new(static () => Build(IntentSlashCatalog.SlashRoutes));

    internal enum CompletionStep
    {
        Domain,
        Object,
        Intent,
        Arg,
    }

    internal readonly record struct CompletionState(
        CompletionStep Step,
        string? Domain,
        string? Object,
        string PartialToken);

    internal static IReadOnlyList<ChatSlashSuggestion> GetSegmentSuggestions(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        string typedBody)
    {
        if (SlashLineResolver.TryResolveBody(typedBody, out var line) && line.ShouldHideSegmentSuggestions)
            return [];

        var state = ResolveCompletionState(tokens, endsWithSpace);
        return state.Step switch
        {
            CompletionStep.Domain => buildDomainSuggestions(state.PartialToken),
            CompletionStep.Object => buildObjectSuggestions(state.Domain!, state.PartialToken, tokens, endsWithSpace),
            CompletionStep.Intent => buildIntentSuggestions(
                state.Domain!,
                state.Object ?? "",
                state.PartialToken,
                tokens,
                endsWithSpace),
            _ => [],
        };
    }

    internal static bool TryResolveHierarchy(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        out SlashSemanticFields fields,
        out string matchedPath)
    {
        fields = default;
        matchedPath = "";
        if (tokens.Count == 0)
            return false;

        if (!tryMatchPrefixByPath(tokens, endsWithSpace, out matchedPath))
            return false;

        if (SlashRouteCatalogIndex.TryGetRoute(matchedPath, out var route))
            fields = route.SemanticFields;
        else
            fields = SlashRouteSemantics.Resolve(matchedPath);

        return true;
    }

    private static CompletionState ResolveCompletionState(IReadOnlyList<string> tokens, bool endsWithSpace)
    {
        var snap = Lazy.Value;
        if (tokens.Count == 0)
            return new(CompletionStep.Domain, null, null, "");

        if (!endsWithSpace)
        {
            if (tokens.Count == 1)
            {
                var t = tokens[0];
                if (snap.DomainsWithCanonicalPrefix.Contains(t))
                    return new(CompletionStep.Object, t, null, "");

                if (snap.ElisionObjectToDomain.TryGetValue(t, out var elisionDomain))
                    return new(CompletionStep.Intent, elisionDomain, t, "");

                return new(CompletionStep.Domain, null, null, t);
            }

            if (tryResolvePrefix(prefixTokens(tokens, dropLast: 1), endsWithSpace: true, out var domain, out var obj)
                && !string.IsNullOrEmpty(obj))
                return new(CompletionStep.Intent, domain, obj, tokens[^1]);

            if (tokens.Count >= 2
                && snap.DomainsWithCanonicalPrefix.Contains(tokens[0])
                && tryResolvePrefix([tokens[0]], endsWithSpace: true, out var domainOnly, out var emptyObj)
                && string.IsNullOrEmpty(emptyObj))
            {
                return tokens.Count == 2
                    ? new(CompletionStep.Object, domainOnly, null, tokens[1])
                    : new(CompletionStep.Intent, domainOnly, "", tokens[^1]);
            }

            return new(CompletionStep.Domain, null, null, tokens[^1]);
        }

        if (tokens.Count == 1)
        {
            var t0 = tokens[0];
            if (snap.DomainsWithCanonicalPrefix.Contains(t0))
                return new(CompletionStep.Object, t0, null, "");

            if (snap.ElisionObjectToDomain.TryGetValue(t0, out var elisionDomain))
                return new(CompletionStep.Intent, elisionDomain, t0, "");

            return new(CompletionStep.Domain, null, null, "");
        }

        if (tokens.Count == 2 && tryResolvePrefix(tokens, endsWithSpace: true, out var d2, out var o2))
        {
            if (!string.IsNullOrEmpty(o2))
                return new(CompletionStep.Intent, d2, o2, "");

            return new(CompletionStep.Arg, d2, "", "");
        }

        if (tokens.Count >= 3 && tryResolvePrefix(tokens, endsWithSpace: true, out var d3, out var o3))
            return new(CompletionStep.Arg, d3, o3, "");

        return new(CompletionStep.Arg, null, null, "");
    }

    private static bool tryResolvePrefix(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        out string? domain,
        out string? obj)
    {
        domain = null;
        obj = null;
        if (tokens.Count == 0)
            return false;

        var snap = Lazy.Value;
        var t0 = tokens[0];

        if (snap.ElisionObjectToDomain.TryGetValue(t0, out var elisionDomain))
        {
            domain = elisionDomain;
            obj = t0;
            if (tokens.Count == 1)
                return true;

            return endsWithSpace;
        }

        if (!snap.DomainsWithCanonicalPrefix.Contains(t0))
            return false;

        domain = t0;
        if (tokens.Count == 1)
        {
            obj = "";
            return true;
        }

        obj = tokens[1];
        return true;
    }

    private static IReadOnlyList<ChatSlashSuggestion> buildDomainSuggestions(string partial)
    {
        var snap = Lazy.Value;
        var buckets = new Dictionary<string, ChatSlashSuggestion>(StringComparer.OrdinalIgnoreCase);

        foreach (var domain in snap.DomainsWithCanonicalPrefix)
        {
            if (!matchesPartial(domain, partial))
                continue;

            addSuggestion(
                buckets,
                domain,
                $"/{domain} ",
                $"/{domain}",
                snap.BestHelpForDomain(domain));
        }

        foreach (var (starter, elisionDomain) in snap.ElisionObjectToDomain)
        {
            if (!matchesPartial(starter, partial))
                continue;

            addSuggestion(
                buckets,
                starter,
                $"/{starter} ",
                $"/{starter}",
                snap.BestHelpForElisionStarter(starter, elisionDomain));
        }

        return order(buckets.Values);
    }

    private static IReadOnlyList<ChatSlashSuggestion> buildObjectSuggestions(
        string domain,
        string partial,
        IReadOnlyList<string> tokens,
        bool endsWithSpace)
    {
        var snap = Lazy.Value;
        var buckets = new Dictionary<string, ChatSlashSuggestion>(StringComparer.OrdinalIgnoreCase);

        if (snap.ObjectsByDomain.TryGetValue(domain, out var objects))
        {
            foreach (var obj in objects)
            {
                if (string.IsNullOrEmpty(obj) || !matchesPartial(obj, partial))
                    continue;

                var insertPath = $"/{domain} {obj} ";
                addSuggestion(
                    buckets,
                    obj,
                    insertPath,
                    insertPath.TrimEnd(),
                    snap.BestHelpForObject(domain, obj));
            }
        }

        if (snap.FlatIntentsByDomain.TryGetValue(domain, out var flatIntents))
        {
            foreach (var (intent, route) in flatIntents)
            {
                if (!matchesPartial(intent, partial))
                    continue;

                var pathSegs = route.PathSegments;
                var insert = buildInsertFromTyped(tokens, endsWithSpace, pathSegs, pathSegs.Count - 1, intent);
                addSuggestion(buckets, intent, insert, route.SlashPath, route.Help, route.Group);
            }
        }

        return order(buckets.Values);
    }

    private static IReadOnlyList<ChatSlashSuggestion> buildIntentSuggestions(
        string domain,
        string obj,
        string partial,
        IReadOnlyList<string> tokens,
        bool endsWithSpace)
    {
        var snap = Lazy.Value;
        var key = (domain, obj);
        if (!snap.RoutesBySemantic.TryGetValue(key, out var routes))
            return [];

        var buckets = new Dictionary<string, ChatSlashSuggestion>(StringComparer.OrdinalIgnoreCase);
        var segmentIndex = endsWithSpace ? tokens.Count : Math.Max(0, tokens.Count - 1);
        foreach (var route in routes)
        {
            var pathSegs = route.PathSegments;
            if (segmentIndex >= pathSegs.Count)
                continue;

            if (!pathPrefixMatches(pathSegs, tokens, endsWithSpace))
                continue;

            var segmentValue = pathSegs[segmentIndex];
            if (!matchesPartial(segmentValue, partial))
                continue;

            var insert = buildInsertFromTyped(tokens, endsWithSpace, pathSegs, segmentIndex, segmentValue);
            addSuggestion(buckets, segmentValue, insert, route.SlashPath, route.Help, route.Group);
        }

        return order(buckets.Values);
    }

    private static void addSuggestion(
        Dictionary<string, ChatSlashSuggestion> buckets,
        string listTitle,
        string insert,
        string slashPath,
        string help,
        string? group = null)
    {
        if (!buckets.TryGetValue(listTitle, out var existing)
            || slashPath.Length > existing.SlashPath.Length)
        {
            buckets[listTitle] = new ChatSlashSuggestion(insert, slashPath, help, group, listTitle);
        }
    }

    private static IReadOnlyList<ChatSlashSuggestion> order(IEnumerable<ChatSlashSuggestion> items) =>
        items.OrderBy(s => ChatSlashCommandCatalog.SortKeyForSuggestion(s.SlashPath)).ToList();

    private static bool matchesPartial(string value, string partial) =>
        partial.Length == 0
        || value.StartsWith(partial, StringComparison.OrdinalIgnoreCase);

    private static string buildInsertFromTyped(
        IReadOnlyList<string> typedTokens,
        bool endsWithSpace,
        IReadOnlyList<string> pathSegs,
        int completeSegmentIndex,
        string segmentValue)
    {
        var resultSegs = new List<string>(completeSegmentIndex + 1);
        for (var i = 0; i < completeSegmentIndex; i++)
            resultSegs.Add(i < typedTokens.Count ? typedTokens[i] : pathSegs[i]);

        resultSegs.Add(segmentValue);
        var slashPath = "/" + string.Join(" ", resultSegs);
        if (completeSegmentIndex + 1 < pathSegs.Count
            || segmentNeedsArgTail(slashPath))
            slashPath += " ";

        return slashPath;
    }

    private static bool segmentNeedsArgTail(string slashPath)
    {
        if (ChatSlashCommandParser.ShouldAutoExecuteAfterAutocompleteCommit(slashPath))
            return false;

        return SlashRouteCatalogIndex.GetArgTailKind(slashPath) != SlashArgTailKind.None;
    }

    private static bool pathPrefixMatches(
        IReadOnlyList<string> pathSegs,
        IReadOnlyList<string> tokens,
        bool endsWithSpace)
    {
        if (tokens.Count == 0)
            return true;

        if (endsWithSpace)
        {
            if (tokens.Count >= pathSegs.Count)
                return false;

            for (var i = 0; i < tokens.Count; i++)
            {
                if (!pathSegs[i].Equals(tokens[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        if (tokens.Count > pathSegs.Count)
            return false;

        for (var i = 0; i < tokens.Count - 1; i++)
        {
            if (!pathSegs[i].Equals(tokens[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return pathSegs[tokens.Count - 1].StartsWith(tokens[^1], StringComparison.OrdinalIgnoreCase);
    }

    private static bool tryMatchPrefixByPath(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        out string matchedPath)
    {
        matchedPath = "";
        var bestLen = -1;

        foreach (var route in Lazy.Value.AllRoutes)
        {
            if (!pathPrefixMatches(route.PathSegments, tokens, endsWithSpace))
                continue;

            if (route.PathSegments.Count <= bestLen)
                continue;

            bestLen = route.PathSegments.Count;
            matchedPath = route.SlashPath;
        }

        return bestLen >= 0;
    }

    private static Snapshot Build(IReadOnlyDictionary<string, SlashRouteEntry> routes)
    {
        var allRoutes = new List<IndexedRoute>();
        var domainsWithCanonicalPrefix = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var elisionObjectToDomain = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var objectsByDomain = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var flatIntentsByDomain = new Dictionary<string, Dictionary<string, IndexedRoute>>(StringComparer.OrdinalIgnoreCase);
        var routesBySemantic = new Dictionary<(string Domain, string Object), List<IndexedRoute>>();

        var helpDomain = new Dictionary<string, (string Help, int Len)>(StringComparer.OrdinalIgnoreCase);
        var helpElision = new Dictionary<string, (string Help, int Len)>(StringComparer.OrdinalIgnoreCase);
        var helpObject = new Dictionary<(string, string), (string Help, int Len)>();

        foreach (var route in routes.Values)
        {
            var sem = route.SemanticFields;
            var pathSegs = splitPath(route.SlashPath);
            if (pathSegs.Count == 0)
                continue;

            var indexed = new IndexedRoute(route, sem, pathSegs);
            allRoutes.Add(indexed);

            var domain = sem.Domain;
            var obj = sem.Object ?? "";
            var key = (domain, obj);

            if (!routesBySemantic.TryGetValue(key, out var list))
            {
                list = [];
                routesBySemantic[key] = list;
            }

            list.Add(indexed);

            if (sem.DomainOmittedInPath && !string.IsNullOrEmpty(obj))
                elisionObjectToDomain[obj] = domain;
            else if (!string.IsNullOrEmpty(domain))
                domainsWithCanonicalPrefix.Add(domain);

            if (!objectsByDomain.TryGetValue(domain, out var objects))
            {
                objects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                objectsByDomain[domain] = objects;
            }

            if (!string.IsNullOrEmpty(obj))
                objects.Add(obj);

            if (string.IsNullOrEmpty(obj) && !string.IsNullOrEmpty(sem.Intent))
            {
                if (!flatIntentsByDomain.TryGetValue(domain, out var flat))
                {
                    flat = new Dictionary<string, IndexedRoute>(StringComparer.OrdinalIgnoreCase);
                    flatIntentsByDomain[domain] = flat;
                }

                if (!flat.TryGetValue(sem.Intent, out var existing)
                    || route.SlashPath.Length > existing.SlashPath.Length)
                    flat[sem.Intent] = indexed;
            }

            trackHelp(helpDomain, domain, route);
            if (sem.DomainOmittedInPath && !string.IsNullOrEmpty(obj))
                trackHelp(helpElision, obj, route);
            if (!string.IsNullOrEmpty(obj))
                trackHelp(helpObject, (domain, obj), route);
        }

        return new Snapshot(
            allRoutes,
            domainsWithCanonicalPrefix,
            elisionObjectToDomain,
            objectsByDomain,
            flatIntentsByDomain,
            routesBySemantic,
            helpDomain,
            helpElision,
            helpObject);
    }

    private static void trackHelp<T>(
        Dictionary<T, (string Help, int Len)> map,
        T key,
        SlashRouteEntry route)
        where T : notnull
    {
        if (!map.TryGetValue(key, out var existing) || route.SlashPath.Length > existing.Len)
            map[key] = (route.Help, route.SlashPath.Length);
    }

    private static IReadOnlyList<string> prefixTokens(IReadOnlyList<string> tokens, int dropLast)
    {
        if (dropLast <= 0)
            return tokens;

        var count = tokens.Count - dropLast;
        if (count <= 0)
            return [];

        var result = new List<string>(count);
        for (var i = 0; i < count; i++)
            result.Add(tokens[i]);

        return result;
    }

    private static List<string> splitPath(string slashPath)
    {
        if (slashPath.Length < 2 || slashPath[0] != '/')
            return [];

        return slashPath[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private sealed record IndexedRoute(
        SlashRouteEntry Route,
        SlashSemanticFields Semantics,
        List<string> PathSegments)
    {
        public string SlashPath => Route.SlashPath;
        public string Help => Route.Help;
        public string? Group => Route.Group;
    }

    private sealed class Snapshot
    {
        public Snapshot(
            List<IndexedRoute> allRoutes,
            HashSet<string> domainsWithCanonicalPrefix,
            Dictionary<string, string> elisionObjectToDomain,
            Dictionary<string, HashSet<string>> objectsByDomain,
            Dictionary<string, Dictionary<string, IndexedRoute>> flatIntentsByDomain,
            Dictionary<(string Domain, string Object), List<IndexedRoute>> routesBySemantic,
            Dictionary<string, (string Help, int Len)> helpDomain,
            Dictionary<string, (string Help, int Len)> helpElision,
            Dictionary<(string, string), (string Help, int Len)> helpObject)
        {
            AllRoutes = allRoutes;
            DomainsWithCanonicalPrefix = domainsWithCanonicalPrefix;
            ElisionObjectToDomain = elisionObjectToDomain;
            ObjectsByDomain = objectsByDomain;
            FlatIntentsByDomain = flatIntentsByDomain;
            RoutesBySemantic = routesBySemantic;
            _helpDomain = helpDomain;
            _helpElision = helpElision;
            _helpObject = helpObject;
        }

        public List<IndexedRoute> AllRoutes { get; }
        public HashSet<string> DomainsWithCanonicalPrefix { get; }
        public Dictionary<string, string> ElisionObjectToDomain { get; }
        public Dictionary<string, HashSet<string>> ObjectsByDomain { get; }
        public Dictionary<string, Dictionary<string, IndexedRoute>> FlatIntentsByDomain { get; }
        public Dictionary<(string Domain, string Object), List<IndexedRoute>> RoutesBySemantic { get; }

        private readonly Dictionary<string, (string Help, int Len)> _helpDomain;
        private readonly Dictionary<string, (string Help, int Len)> _helpElision;
        private readonly Dictionary<(string, string), (string Help, int Len)> _helpObject;

        public string BestHelpForDomain(string domain) =>
            _helpDomain.TryGetValue(domain, out var h) ? h.Help : domain;

        public string BestHelpForElisionStarter(string starter, string domain) =>
            _helpElision.TryGetValue(starter, out var h) ? h.Help : $"{starter} ({domain})";

        public string BestHelpForObject(string domain, string obj) =>
            _helpObject.TryGetValue((domain, obj), out var h) ? h.Help : obj;
    }
}
