using CascadeIDE.Features.Chat;

namespace CascadeIDE.Services;

/// <summary>
/// Единый intent-каталог: <c>command_id</c> и формы <b>melody</b> (<c>c:</c>) / <b>slash</b> (<c>/</c>).
/// Источник: <see cref="BundledRelativePath"/> (оверлей рядом с exe, иначе embedded), ADR 0109, ADR 0119.
/// </summary>
public static class IntentMelodyAliases
{
    /// <summary>Относительно <see cref="AppContext.BaseDirectory"/>; оверлей поверх встроенного TOML.</summary>
    public const string BundledRelativePath = "IntentMelody/intent-catalog.toml";

    /// <summary>Устаревшее имя файла оверлея (читается, если <see cref="BundledRelativePath"/> отсутствует на диске).</summary>
    public const string LegacyBundledRelativePath = "IntentMelody/intent-melody-aliases.toml";

#if DEBUG
    private static Lazy<IntentMelodyBundleState>? _bundleLazy;
#else
    private static readonly Lazy<IntentMelodyBundleState> BundleLazyFixed = new(LoadBundle, LazyThreadSafetyMode.ExecutionAndPublication);
#endif

    internal static Lazy<IntentMelodyBundleState> BundleLazy =>
#if DEBUG
        _bundleLazy ??= new Lazy<IntentMelodyBundleState>(LoadBundle, LazyThreadSafetyMode.ExecutionAndPublication);
#else
        BundleLazyFixed;
#endif

    internal sealed class IntentMelodyTomlRoot
    {
        public int? IntentCatalogSchemaVersion { get; set; }
        public int? MelodyCatalogSchemaVersion { get; set; }
        public int? SlashCatalogSchemaVersion { get; set; }
        public Dictionary<string, string>? Aliases { get; set; }
        public List<CommandToml>? Command { get; set; }
        public List<MelodyRootToml>? MelodyRoot { get; set; }
        public List<SlashRouteToml>? SlashRoute { get; set; }
        public List<TailWireClassToml>? TailWireClass { get; set; }
    }

    /// <remarks>Legacy: <c>[[melody_root]]</c>.</remarks>
    internal sealed class MelodyRootToml
    {
        public string? Slug { get; set; }
        public string? CommandId { get; set; }
        public string? Shape { get; set; }
        public bool? ShowUsageHintIfBareSlug { get; set; }
        public string? TailSignature { get; set; }
        public string? WireClass { get; set; }
        public string? ChordCommit { get; set; }
        public string? PaletteHintSlug { get; set; }
        public string? PaletteUsageHint { get; set; }
        public string? PaletteUsageCategory { get; set; }
    }

    /// <remarks>Legacy: <c>[[slash_route]]</c>.</remarks>
    internal sealed class SlashRouteToml
    {
        public string? Path { get; set; }
        public string? CommandId { get; set; }
        public string? Help { get; set; }
        public string? Group { get; set; }
        public string? MfdPage { get; set; }
        public string? PrimarySurface { get; set; }
        public string? Kind { get; set; }
    }

#pragma warning disable CA1812
    internal sealed class TailWireClassToml
    {
        public string? Id { get; set; }
        public string? Kind { get; set; }
        public string[]? BetweenSlotsAnyOf { get; set; }
    }
#pragma warning restore CA1812

    internal sealed record IntentMelodyBundleState(Dictionary<string, string> AliasToCommandId, IntentMelodyCatalogSnapshot Catalog);

    private static Dictionary<string, string> AliasToCommandId => BundleLazy.Value.AliasToCommandId;

    internal static IntentMelodyCatalogSnapshot GetCatalogSnapshot() => BundleLazy.Value.Catalog;

#if DEBUG
    internal static void ResetForTests() => Interlocked.Exchange(ref _bundleLazy, null);
#else
    internal static void ResetForTests() { }
#endif

    private static IntentMelodyBundleState LoadBundle()
    {
        if ((!BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var text)
             && !BundledAppContent.TryReadDiskThenEmbedded(LegacyBundledRelativePath, out text))
            || string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                $"Missing {BundledRelativePath} (file under AppContext.BaseDirectory or embedded resource in CascadeIDE assembly; legacy disk name: {LegacyBundledRelativePath}).");
        }

        var root = CascadeTomlSerializer.Deserialize<IntentMelodyTomlRoot>(text.Trim())
                   ?? throw new InvalidOperationException($"Invalid {BundledRelativePath}: empty parse.");

        return Build(root);
    }

    internal static IntentMelodyBundleState Build(IntentMelodyTomlRoot root)
    {
        var catalog = IntentCatalogLoader.BuildSnapshot(root, BundledRelativePath);
        var commandMap = IntentCatalogLoader.BuildMelodyAliasMap(catalog);

        if (commandMap.Count == 0 && catalog.SlashRoutes.Count == 0)
        {
            throw new InvalidOperationException(
                $"{BundledRelativePath}: пустой каталог (нет melody slug и slash).");
        }

        return new IntentMelodyBundleState(commandMap, catalog);
    }

    internal static IntentMelodyBundleState ParseBundleForTests(string tomlTrimmed) =>
        Build(CascadeTomlSerializer.Deserialize<IntentMelodyTomlRoot>(tomlTrimmed)
              ?? throw new InvalidOperationException("empty parse"));

    public static IReadOnlyList<(string Alias, string CommandId)> AllPairs() =>
        AliasToCommandId
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();

    public static string SampleAliasesForFooter(int maxAliases = 8)
    {
        if (maxAliases <= 0)
            return "";

        var pairs = AllPairs();
        if (pairs.Count == 0)
            return "";

        var take = Math.Min(maxAliases, pairs.Count);
        var s = string.Join(", ", pairs.Take(take).Select(p => p.Alias));
        if (pairs.Count > maxAliases)
            s += ", …";
        return s;
    }

    public static IReadOnlyList<(string Alias, string CommandId)> FilterByTailPrefix(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
            return AllPairs();

        var list = new List<(string, string)>();
        foreach (var kv in AliasToCommandId.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (kv.Key.StartsWith(tailNormalized, StringComparison.Ordinal))
                list.Add((kv.Key, kv.Value));
        }

        return list;
    }

    public static bool TryGetTail(string? raw, out string tailNormalized)
    {
        tailNormalized = "";
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var t = raw.TrimStart();
        if (t.Length < 2 || char.ToLowerInvariant(t[0]) != 'c' || t[1] != ':')
            return false;

        tailNormalized = t.Length > 2 ? t[2..].Trim().ToLowerInvariant() : "";
        return true;
    }

    public static string? TryResolveExactCommandId(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
            return null;

        return AliasToCommandId.TryGetValue(tailNormalized, out var id) ? id : null;
    }

    public static bool HasStrictLongerAliasPrefix(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
            return false;

        foreach (var key in AliasToCommandId.Keys)
        {
            if (key.Length > tailNormalized.Length && key.StartsWith(tailNormalized, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
