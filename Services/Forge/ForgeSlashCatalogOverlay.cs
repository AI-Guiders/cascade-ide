#nullable enable

using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;

namespace CascadeIDE.Services.Forge;

/// <summary>Runtime overlay of forge <c>capabilities.commands[]</c> into CIDE slash catalog (Phase D).</summary>
public static class ForgeSlashCatalogOverlay
{
    private static readonly object Gate = new();
    private static string? _baseUrl;
    private static Dictionary<string, SlashRouteEntry> _routes = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, ChatSlashCommandDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private static HashSet<string> _pathSet = new(StringComparer.OrdinalIgnoreCase);
    private static string[] _pathsLongestFirst = [];

    public static bool IsActive
    {
        get
        {
            lock (Gate)
                return _routes.Count > 0;
        }
    }

    public static async Task<(bool Ok, string Message)> RefreshAsync(
        string baseUrl,
        string? apiToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commands = await ForgeCapabilitiesClient.FetchCommandsAsync(baseUrl, apiToken, cancellationToken)
                .ConfigureAwait(false);
            Apply(baseUrl.Trim().TrimEnd('/'), commands);
            return (true, $"Forge slash catalog: {commands.Count} command(s).");
        }
        catch (Exception ex)
        {
            Clear(baseUrl);
            return (false, ex.Message);
        }
    }

    public static void Clear(string? baseUrl = null)
    {
        lock (Gate)
        {
            if (baseUrl is not null
                && _baseUrl is not null
                && !string.Equals(_baseUrl, baseUrl.Trim().TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _baseUrl = null;
            _routes = new Dictionary<string, SlashRouteEntry>(StringComparer.OrdinalIgnoreCase);
            _descriptors = new Dictionary<string, ChatSlashCommandDescriptor>(StringComparer.OrdinalIgnoreCase);
            _pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _pathsLongestFirst = [];
        }
    }

    internal static void ApplyForTests(IReadOnlyList<ForgeCapabilitiesCommand> commands) =>
        Apply("http://forge.test", commands);

    internal static void Apply(string baseUrl, IReadOnlyList<ForgeCapabilitiesCommand> commands)
    {
        var routes = new Dictionary<string, SlashRouteEntry>(StringComparer.OrdinalIgnoreCase);
        var descriptors = new Dictionary<string, ChatSlashCommandDescriptor>(StringComparer.OrdinalIgnoreCase);
        var pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var command in commands)
        {
            foreach (var slashPath in SelectCidePaths(command))
            {
                if (routes.ContainsKey(slashPath))
                    continue;

                var route = ToRouteEntry(command, slashPath);
                routes[slashPath] = route;
                descriptors[slashPath] = ToDescriptor(route);
                pathSet.Add(slashPath);
            }
        }

        var longestFirst = pathSet.OrderByDescending(static p => p.Length).ToArray();

        lock (Gate)
        {
            _baseUrl = baseUrl;
            _routes = routes;
            _descriptors = descriptors;
            _pathSet = pathSet;
            _pathsLongestFirst = longestFirst;
        }
    }

    public static bool TryResolveLongestPrefix(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        out string canonicalPath,
        out string argTail,
        out bool isExactPath,
        out bool endsWithSpaceAfterPath)
    {
        canonicalPath = "";
        argTail = "";
        isExactPath = false;
        endsWithSpaceAfterPath = false;
        if (tokens.Count == 0)
            return false;

        string[] paths;
        lock (Gate)
            paths = _pathsLongestFirst;

        if (paths.Length == 0)
            return false;

        for (var len = tokens.Count; len >= 1; len--)
        {
            var path = "/" + string.Join(' ', tokens.Take(len));
            lock (Gate)
            {
                if (!_pathSet.Contains(path))
                    continue;
            }

            canonicalPath = path;
            argTail = string.Join(' ', tokens.Skip(len));
            var hasArgTail = argTail.Length > 0;
            isExactPath = len == tokens.Count && !endsWithSpace && !hasArgTail;
            endsWithSpaceAfterPath = endsWithSpace && !hasArgTail && len == tokens.Count;
            return true;
        }

        return false;
    }

    public static bool TryGetRoute(string slashPath, out SlashRouteEntry route)
    {
        lock (Gate)
            return _routes.TryGetValue(IntentSlashCatalog.NormalizeSlashPath(slashPath), out route);
    }

    public static bool TryGetDescriptor(string slashPath, out ChatSlashCommandDescriptor descriptor)
    {
        lock (Gate)
            return _descriptors.TryGetValue(IntentSlashCatalog.NormalizeSlashPath(slashPath), out descriptor);
    }

    public static SlashArgTailKind GetArgTailKind(string slashPath)
    {
        if (!TryGetRoute(slashPath, out var route))
            return SlashArgTailKind.None;

        return route.ArgTailKindExplicit ?? SlashArgTailKind.None;
    }

    internal static IEnumerable<SlashRouteEntry> AllRoutes
    {
        get
        {
            lock (Gate)
                return _routes.Values.ToList();
        }
    }

    private static IEnumerable<string> SelectCidePaths(ForgeCapabilitiesCommand command)
    {
        var paths = new List<string>();
        foreach (var alias in command.PathAliases)
        {
            var normalized = IntentSlashCatalog.NormalizeSlashPath(alias);
            if (normalized.StartsWith("/forge ", StringComparison.OrdinalIgnoreCase))
                paths.Add(normalized);
        }

        if (paths.Count > 0)
            return paths;

        var obj = command.Object.Replace("_", " ", StringComparison.Ordinal);
        return [IntentSlashCatalog.NormalizeSlashPath($"/forge {obj} {command.Intent}")];
    }

    private static SlashRouteEntry ToRouteEntry(ForgeCapabilitiesCommand command, string slashPath) =>
        new(
            slashPath,
            command.CommandId,
            command.Help?.Trim() ?? command.CommandId,
            ChatSlashCommandExecutionKind.ForgeCommand,
            Group: string.IsNullOrWhiteSpace(command.Category) ? "Forge" : command.Category.Trim(),
            ArgTailKindExplicit: ParseArgTail(command.ArgTail),
            Domain: command.Domain,
            Object: command.Object,
            Intent: command.Intent);

    private static ChatSlashCommandDescriptor ToDescriptor(SlashRouteEntry route) =>
        new(
            route.SlashPath,
            route.CommandId,
            route.Help,
            route.ExecutionKind,
            SlashGroup: route.Group);

    private static SlashArgTailKind ParseArgTail(string? raw) =>
        (raw ?? "optional").Trim().ToLowerInvariant() switch
        {
            "none" => SlashArgTailKind.None,
            "required" => SlashArgTailKind.Required,
            _ => SlashArgTailKind.Optional,
        };
}
