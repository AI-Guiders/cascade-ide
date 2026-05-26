#nullable enable
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Слэш Intercom → <see cref="IdeCommands"/> (ADR 0119).
/// Источник: <c>[[command]]</c> → <c>[[command.slash]]</c> в <see cref="IntentMelodyAliases.BundledRelativePath"/> (якорь <c>command_id</c>, ADR 0109/0119).
/// Оверлей TOML рядом с exe — без пересборки для новых команд.
/// </summary>
public static partial class ChatSlashCommandCatalog
{
    private static readonly Lazy<CatalogSnapshot> SnapshotLazy = new(static () => BuildSnapshot());

    private static CatalogSnapshot Snapshot => SnapshotLazy.Value;

    public static bool TryResolve(ChatSlashCommandParseResult parse, out ChatSlashCommandDescriptor descriptor)
    {
        descriptor = null!;
        if (!parse.IsSlashLine || parse.IsRejected)
            return false;

        if (parse.Shape == ChatSlashCommandShape.Flat)
        {
            if (IntercomSlashPathBuilder.TryBuildPath(parse, out var intercomFlatPath)
                && Snapshot.ByPath.TryGetValue(intercomFlatPath, out descriptor))
            {
                return true;
            }

            var flat = "/" + parse.Head;
            if (Snapshot.ByPath.TryGetValue(flat, out descriptor))
                return true;

            if (TryResolveFlatHelp(flat, out descriptor))
                return true;

            return false;
        }

        if (IntercomSlashPathBuilder.TryBuildPath(parse, out var intercomPath)
            && Snapshot.ByPath.TryGetValue(intercomPath, out descriptor))
        {
            return true;
        }

        var path = string.IsNullOrEmpty(parse.SubAction)
            ? "/" + parse.Head + " " + parse.Action
            : "/" + parse.Head + " " + parse.Action + " " + parse.SubAction;

        return Snapshot.ByPath.TryGetValue(path, out descriptor);
    }

    /// <summary>Резолв по каноническому пути и хвосту (ADR 0150), без дублирования intercom strip.</summary>
    public static bool TryResolveCanonical(string canonicalPath, string? argTail, out ChatSlashCommandDescriptor descriptor)
    {
        descriptor = null!;
        var line = string.IsNullOrWhiteSpace(argTail)
            ? canonicalPath
            : $"{canonicalPath} {argTail.Trim()}";
        return TryResolve(ChatSlashCommandParser.TryParse(line), out descriptor);
    }

    private static bool TryResolveFlatHelp(string flat, out ChatSlashCommandDescriptor descriptor)
    {
        if (!Snapshot.ByPath.TryGetValue(flat, out descriptor))
        {
            descriptor = null!;
            return false;
        }

        if (descriptor.ExecutionKind == ChatSlashCommandExecutionKind.LocalHelp)
            return true;

        return !descriptor.SlashPath.Contains(' ', StringComparison.Ordinal);
    }

    public static IReadOnlyList<ChatSlashSuggestion> AllSuggestions() =>
        OrderDescriptors(Snapshot.Descriptors)
            .Select(e => new ChatSlashSuggestion(
                e.SlashPath,
                e.SlashPath,
                e.Help,
                ResolveGroup(e)))
            .ToList();

    public static IReadOnlyList<string> ListHelpLines(string? namespaceFilter = null)
    {
        var lines = new List<string>
        {
            string.IsNullOrWhiteSpace(namespaceFilter)
                ? "Слэш-команды Intercom (Tab — autocomplete, Enter — выполнить):"
                : $"Слэш-команды /{namespaceFilter.Trim()}:*",
        };

        foreach (var entry in OrderDescriptors(Snapshot.Descriptors))
        {
            if (!string.IsNullOrWhiteSpace(namespaceFilter)
                && !entry.SlashPath.StartsWith("/" + namespaceFilter.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            lines.Add($"  {entry.SlashPath} — {entry.Help}");
        }

        return lines;
    }

    internal static string? GroupFor(string slashPath)
    {
        if (!IntentSlashCatalog.TryGetRoute(slashPath, out var route))
            return ExtractNamespaceHead(slashPath);

        return string.IsNullOrWhiteSpace(route.Group) ? ExtractNamespaceHead(slashPath) : route.Group;
    }

    internal static string SortKeyForSuggestion(ChatSlashCommandDescriptor descriptor) =>
        SortKey(descriptor);

    internal static string SortKeyForSuggestion(string slashPath) =>
        IntentSlashCatalog.TryGetRoute(slashPath, out var route)
            ? SortKey(ToDescriptor(route))
            : slashPath;

    internal static IEnumerable<ChatSlashCommandDescriptor> OrderDescriptors(
        IEnumerable<ChatSlashCommandDescriptor> descriptors) =>
        descriptors.OrderBy(SortKey, StringComparer.Ordinal);

    private static string? ResolveGroup(ChatSlashCommandDescriptor descriptor) =>
        string.IsNullOrWhiteSpace(descriptor.SlashGroup)
            ? ExtractNamespaceHead(descriptor.SlashPath)
            : descriptor.SlashGroup;

    private static ChatSlashCommandDescriptor ToDescriptor(SlashRouteEntry route) =>
        new(
            route.SlashPath,
            route.CommandId,
            route.Help,
            route.ExecutionKind,
            route.MfdPage,
            route.PrimarySurface,
            route.Group,
            route.Completion,
            route.MessageAudience,
            route.AutoRunOnCommit,
            route.AutoRunRequiresArgs);

    internal static bool TryGetRoute(string slashPath, out SlashRouteEntry route) =>
        SlashRouteCatalogIndex.TryGetRoute(slashPath, out route);

    private static CatalogSnapshot BuildSnapshot()
    {
        var routes = IntentSlashCatalog.SlashRoutes;
        var descriptors = routes.Values.Select(ToDescriptor).ToList();
        var byPath = new Dictionary<string, ChatSlashCommandDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in descriptors)
            byPath[d.SlashPath] = d;

        return new CatalogSnapshot(descriptors, byPath);
    }

    private static string SortKey(ChatSlashCommandDescriptor descriptor)
    {
        var group = ResolveGroup(descriptor) ?? "";
        return $"{group}\u001f{descriptor.SlashPath}";
    }

    private static string? ExtractNamespaceHead(string slashPath)
    {
        if (slashPath.Length < 2 || slashPath[0] != '/')
            return null;

        var body = slashPath[1..];
        var space = body.IndexOf(' ');
        return space < 0 ? body : body[..space];
    }

    private sealed record CatalogSnapshot(
        IReadOnlyList<ChatSlashCommandDescriptor> Descriptors,
        Dictionary<string, ChatSlashCommandDescriptor> ByPath);
}
