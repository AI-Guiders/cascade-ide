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
    private static readonly Lazy<IReadOnlyList<ChatSlashCommandDescriptor>> DescriptorsLazy = new(
        static () => IntentSlashCatalog.SlashRoutes.Values.Select(ToDescriptor).ToList());

    private static IReadOnlyList<ChatSlashCommandDescriptor> Descriptors => DescriptorsLazy.Value;

    public static bool TryResolve(ChatSlashCommandParseResult parse, out ChatSlashCommandDescriptor descriptor)
    {
        descriptor = null!;
        if (!parse.IsSlashLine || parse.IsRejected)
            return false;

        if (parse.Shape == ChatSlashCommandShape.Flat)
        {
            if (IntercomSlashPathBuilder.TryBuildPath(parse, out var intercomFlatPath))
            {
                foreach (var entry in Descriptors)
                {
                    if (string.Equals(entry.SlashPath, intercomFlatPath, StringComparison.OrdinalIgnoreCase))
                    {
                        descriptor = entry;
                        return true;
                    }
                }
            }

            var flat = "/" + parse.Head;
            foreach (var entry in Descriptors)
            {
                if (entry.ExecutionKind == ChatSlashCommandExecutionKind.LocalHelp
                    && string.Equals(entry.SlashPath, flat, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor = entry;
                    return true;
                }

                if (!entry.SlashPath.Contains(' ', StringComparison.Ordinal)
                    && string.Equals(entry.SlashPath, flat, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor = entry;
                    return true;
                }
            }

            return false;
        }

        if (IntercomSlashPathBuilder.TryBuildPath(parse, out var intercomPath))
        {
            foreach (var entry in Descriptors)
            {
                if (string.Equals(entry.SlashPath, intercomPath, StringComparison.OrdinalIgnoreCase))
                {
                    descriptor = entry;
                    return true;
                }
            }
        }

        var path = string.IsNullOrEmpty(parse.SubAction)
            ? "/" + parse.Head + " " + parse.Action
            : "/" + parse.Head + " " + parse.Action + " " + parse.SubAction;
        foreach (var entry in Descriptors)
        {
            if (string.Equals(entry.SlashPath, path, StringComparison.OrdinalIgnoreCase))
            {
                descriptor = entry;
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<ChatSlashSuggestion> AllSuggestions() =>
        OrderDescriptors(Descriptors)
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

        foreach (var entry in OrderDescriptors(Descriptors))
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
            route.MessageAudience);

    internal static bool TryGetRoute(string slashPath, out SlashRouteEntry route) =>
        IntentSlashCatalog.TryGetRoute(slashPath, out route);

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
}
