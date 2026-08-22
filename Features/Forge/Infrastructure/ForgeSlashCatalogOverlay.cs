#nullable enable

using AIGuiders.Platform.CommandPlane;
using CascadeIDE.Features.Chat;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Forge.Infrastructure;

/// <summary>Runtime overlay of forge <c>capabilities.commands[]</c> into CIDE slash catalog (Phase D).</summary>
public static class ForgeSlashCatalogOverlay
{
    private static readonly object Gate = new();
    private static string? _baseUrl;
    private static SlashCatalogIndex _catalog = SlashCatalogIndex.Empty;
    private static Dictionary<string, CascadeIDE.Services.SlashRouteEntry> _routes = new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, ChatSlashCommandDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);

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
            _catalog = SlashCatalogIndex.Empty;
            _routes = new Dictionary<string, CascadeIDE.Services.SlashRouteEntry>(StringComparer.OrdinalIgnoreCase);
            _descriptors = new Dictionary<string, ChatSlashCommandDescriptor>(StringComparer.OrdinalIgnoreCase);
        }
    }

    internal static void ApplyForTests(IReadOnlyList<SlashCommandDescriptor> commands) =>
        Apply("http://forge.test", commands);

    internal static void Apply(string baseUrl, IReadOnlyList<SlashCommandDescriptor> commands)
    {
        var overlayDescriptors = new List<SlashCommandDescriptor>();
        var routes = new Dictionary<string, CascadeIDE.Services.SlashRouteEntry>(StringComparer.OrdinalIgnoreCase);
        var descriptors = new Dictionary<string, ChatSlashCommandDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var command in commands)
        {
            foreach (var slashPath in SelectCidePaths(command))
            {
                if (routes.ContainsKey(slashPath))
                    continue;

                var overlayDescriptor = ForCidePath(command, slashPath);
                overlayDescriptors.Add(overlayDescriptor);

                var route = ToCideRoute(overlayDescriptor, slashPath);
                routes[slashPath] = route;
                descriptors[slashPath] = ToChatDescriptor(route);
            }
        }

        var catalog = SlashCatalogIndex.FromDescriptors(overlayDescriptors);

        lock (Gate)
        {
            _baseUrl = baseUrl;
            _catalog = catalog;
            _routes = routes;
            _descriptors = descriptors;
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

        SlashCatalogIndex catalog;
        lock (Gate)
            catalog = _catalog;

        if (!catalog.TryResolveLongestPrefix(
                tokens,
                endsWithSpace,
                out var pathBody,
                out argTail,
                out _,
                out _,
                out _))
        {
            return false;
        }

        canonicalPath = IntentSlashCatalog.NormalizeSlashPath(pathBody);
        var pathTokenCount = pathBody.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var hasArgTail = argTail.Length > 0;
        isExactPath = pathTokenCount == tokens.Count && !endsWithSpace && !hasArgTail;
        endsWithSpaceAfterPath = endsWithSpace && !hasArgTail && pathTokenCount == tokens.Count;
        return true;
    }

    public static bool TryGetRoute(string slashPath, out CascadeIDE.Services.SlashRouteEntry route)
    {
        lock (Gate)
            return _routes.TryGetValue(IntentSlashCatalog.NormalizeSlashPath(slashPath), out route);
    }

    public static bool TryGetDescriptor(string slashPath, out ChatSlashCommandDescriptor descriptor)
    {
        lock (Gate)
            return _descriptors.TryGetValue(IntentSlashCatalog.NormalizeSlashPath(slashPath), out descriptor);
    }

    public static CascadeIDE.Features.Chat.SlashArgTailKind GetArgTailKind(string slashPath)
    {
        if (!TryGetRoute(slashPath, out var route))
            return CascadeIDE.Features.Chat.SlashArgTailKind.None;

        return route.ArgTailKindExplicit ?? CascadeIDE.Features.Chat.SlashArgTailKind.None;
    }

    internal static IEnumerable<CascadeIDE.Services.SlashRouteEntry> AllRoutes
    {
        get
        {
            lock (Gate)
                return _routes.Values.ToList();
        }
    }

    internal static IEnumerable<string> SelectCidePaths(SlashCommandDescriptor command)
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

    internal static SlashCommandDescriptor ForCidePath(SlashCommandDescriptor source, string cideSlashPath)
    {
        var body = cideSlashPath.Trim();
        if (body.StartsWith('/'))
            body = body[1..];

        return new SlashCommandDescriptor
        {
            Domain = source.Domain,
            Object = source.Object,
            Intent = source.Intent,
            CommandId = source.CommandId,
            Path = body,
            Help = source.Help,
            Group = source.Group,
            ArgTail = source.ArgTail,
            ArgHint = source.ArgHint,
            ArgPickerChoices = source.ArgPickerChoices,
            Surfaces = source.Surfaces,
            RequiredCapabilities = source.RequiredCapabilities,
            Tier = source.Tier,
            PluginId = source.PluginId,
            RequiresDestructiveConfirm = source.RequiresDestructiveConfirm,
        };
    }

    private static CascadeIDE.Services.SlashRouteEntry ToCideRoute(SlashCommandDescriptor command, string slashPath) =>
        new(
            slashPath,
            command.CommandId,
            command.Help?.Trim() ?? command.CommandId,
            ChatSlashCommandExecutionKind.ForgeCommand,
            Group: string.IsNullOrWhiteSpace(command.Group) ? "Forge" : command.Group.Trim(),
            ArgTailKindExplicit: ToCideArgTail(command.ArgTailKind),
            Domain: command.Domain,
            Object: command.Object,
            Intent: command.Intent);

    private static ChatSlashCommandDescriptor ToChatDescriptor(CascadeIDE.Services.SlashRouteEntry route) =>
        new(
            route.SlashPath,
            route.CommandId,
            route.Help,
            route.ExecutionKind,
            SlashGroup: route.Group);

    private static CascadeIDE.Features.Chat.SlashArgTailKind ToCideArgTail(AIGuiders.Platform.CommandPlane.SlashArgTailKind kind) =>
        kind switch
        {
            AIGuiders.Platform.CommandPlane.SlashArgTailKind.None => CascadeIDE.Features.Chat.SlashArgTailKind.None,
            AIGuiders.Platform.CommandPlane.SlashArgTailKind.Required => CascadeIDE.Features.Chat.SlashArgTailKind.Required,
            _ => CascadeIDE.Features.Chat.SlashArgTailKind.Optional,
        };
}
