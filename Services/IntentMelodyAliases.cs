using CascadeIDE.Features.Chat;
using Tomlyn;
using Tomlyn.Model;

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

        var root = ParseIntentCatalogToRoot(text.Trim(), BundledRelativePath);

        return Build(root);
    }

    private static IntentMelodyTomlRoot ParseIntentCatalogToRoot(string tomlTrimmed, string sourceName)
    {
        // Tomlyn reflection deserializer is fragile for command-first TOML with nested arrays-of-tables.
        // We parse to DOM (TomlTable) and project into our POCOs explicitly.
        var rootTable = TomlSerializer.Deserialize<TomlTable>(tomlTrimmed, CascadeTomlSerializer.Options);
        if (rootTable is null)
            throw new InvalidOperationException($"{sourceName}: empty parse.");

        return new IntentMelodyTomlRoot
        {
            IntentCatalogSchemaVersion = GetInt(rootTable, "intent_catalog_schema_version"),
            MelodyCatalogSchemaVersion = GetInt(rootTable, "melody_catalog_schema_version"),
            SlashCatalogSchemaVersion = GetInt(rootTable, "slash_catalog_schema_version"),
            Aliases = GetStringMap(rootTable, "aliases"),
            Command = ReadCommands(rootTable),
            // legacy blocks (melody_root, slash_route, tail_wire_class) are still supported if present
            MelodyRoot = ReadMelodyRoots(rootTable),
            SlashRoute = ReadSlashRoutes(rootTable),
            TailWireClass = ReadTailWireClasses(rootTable),
        };
    }

    private static List<CommandToml>? ReadCommands(TomlTable root)
    {
        if (!root.TryGetValue("command", out var v) || v is not TomlTableArray arr || arr.Count == 0)
            return null;

        var list = new List<CommandToml>(arr.Count);
        foreach (var x in arr)
        {
            if (x is not TomlTable t)
                continue;

            var cmd = new CommandToml
            {
                CommandId = GetString(t, "command_id"),
                Enabled = GetBool(t, "enabled"),
                SlashGroup = GetString(t, "slash_group"),

                MelodySlug = GetString(t, "melody_slug"),
                MelodyShape = GetString(t, "melody_shape"),
                MelodyShowUsageHintIfBareSlug = GetBool(t, "melody_show_usage_hint_if_bare_slug"),
                MelodyTailSignature = GetString(t, "melody_tail_signature"),
                MelodyWireClass = GetString(t, "melody_wire_class"),
                MelodyChordCommit = GetString(t, "melody_chord_commit"),
                MelodyPaletteHintSlug = GetString(t, "melody_palette_hint_slug"),
                MelodyPaletteUsageHint = GetString(t, "melody_palette_usage_hint"),
                MelodyPaletteUsageCategory = GetString(t, "melody_palette_usage_category"),
            };

            // legacy [command.melody]
            cmd.Melody = ReadMelodyForm(t, "melody");

            // legacy [[command.slash]] (what we use now)
            cmd.Slash = ReadSlashForms(t, "slash");

            // newer nested [command.form] with [[command.form.slash]] if present in older overlays
            if (t.TryGetValue("form", out var formObj) && formObj is TomlTable form)
            {
                cmd.Form = new CommandFormToml
                {
                    Melody = ReadMelodyForm(form, "melody"),
                    Slash = ReadSlashForms(form, "slash"),
                };
            }

            list.Add(cmd);
        }

        return list.Count == 0 ? null : list;
    }

    private static MelodyFormToml? ReadMelodyForm(TomlTable t, string key)
    {
        if (!t.TryGetValue(key, out var v) || v is not TomlTable m)
            return null;

        return new MelodyFormToml
        {
            Slug = GetString(m, "slug"),
            Shape = GetString(m, "shape"),
            ShowUsageHintIfBareSlug = GetBool(m, "show_usage_hint_if_bare_slug"),
            TailSignature = GetString(m, "tail_signature"),
            WireClass = GetString(m, "wire_class"),
            ChordCommit = GetString(m, "chord_commit"),
            PaletteHintSlug = GetString(m, "palette_hint_slug"),
            PaletteUsageHint = GetString(m, "palette_usage_hint"),
            PaletteUsageCategory = GetString(m, "palette_usage_category"),
        };
    }

    private static List<SlashFormToml>? ReadSlashForms(TomlTable t, string key)
    {
        if (!t.TryGetValue(key, out var v) || v is not TomlTableArray arr || arr.Count == 0)
            return null;

        var list = new List<SlashFormToml>(arr.Count);
        foreach (var x in arr)
        {
            if (x is not TomlTable s)
                continue;

            var sf = new SlashFormToml
            {
                Enabled = GetBool(s, "enabled"),
                Path = GetString(s, "path"),
                Help = GetString(s, "help"),
                Group = GetString(s, "group"),
                Kind = GetString(s, "kind"),
                MfdPage = GetString(s, "mfd_page"),
                PrimarySurface = GetString(s, "primary_surface"),
                Completion = GetString(s, "completion"),
                ReportHandler = GetString(s, "report_handler"),
                IntercomHandler = GetString(s, "intercom_handler"),
                Audience = GetString(s, "audience"),
                AutoRunOnCommit = GetBool(s, "auto_run_on_commit"),
                AutoRunRequiresArgs = GetBool(s, "auto_run_requires_args"),
                RequiresArgTail = GetBool(s, "requires_arg_tail"),
                ArgTail = GetString(s, "arg_tail"),
                Domain = GetString(s, "domain"),
                Object = GetString(s, "object"),
                Intent = GetString(s, "intent"),
                PathRole = GetString(s, "path_role"),
            };

            // args = { page=..., surface=..., level=... }
            if (s.TryGetValue("args", out var argsObj) && argsObj is TomlTable a)
            {
                sf.Args = new SlashStaticArgsToml
                {
                    Page = GetString(a, "page"),
                    Surface = GetString(a, "surface"),
                    Level = GetString(a, "level"),
                };
            }

            list.Add(sf);
        }

        return list.Count == 0 ? null : list;
    }

    private static List<MelodyRootToml>? ReadMelodyRoots(TomlTable root)
    {
        if (!root.TryGetValue("melody_root", out var v) || v is not TomlTableArray arr || arr.Count == 0)
            return null;
        var list = new List<MelodyRootToml>(arr.Count);
        foreach (var x in arr)
        {
            if (x is not TomlTable t)
                continue;
            list.Add(new MelodyRootToml
            {
                Slug = GetString(t, "slug"),
                CommandId = GetString(t, "command_id"),
                Shape = GetString(t, "shape"),
                ShowUsageHintIfBareSlug = GetBool(t, "show_usage_hint_if_bare_slug"),
                TailSignature = GetString(t, "tail_signature"),
                WireClass = GetString(t, "wire_class"),
                ChordCommit = GetString(t, "chord_commit"),
                PaletteHintSlug = GetString(t, "palette_hint_slug"),
                PaletteUsageHint = GetString(t, "palette_usage_hint"),
                PaletteUsageCategory = GetString(t, "palette_usage_category"),
            });
        }
        return list.Count == 0 ? null : list;
    }

    private static List<SlashRouteToml>? ReadSlashRoutes(TomlTable root)
    {
        if (!root.TryGetValue("slash_route", out var v) || v is not TomlTableArray arr || arr.Count == 0)
            return null;
        var list = new List<SlashRouteToml>(arr.Count);
        foreach (var x in arr)
        {
            if (x is not TomlTable t)
                continue;
            list.Add(new SlashRouteToml
            {
                Path = GetString(t, "path"),
                CommandId = GetString(t, "command_id"),
                Help = GetString(t, "help"),
                Group = GetString(t, "group"),
                MfdPage = GetString(t, "mfd_page"),
                PrimarySurface = GetString(t, "primary_surface"),
                Kind = GetString(t, "kind"),
            });
        }
        return list.Count == 0 ? null : list;
    }

    private static List<TailWireClassToml>? ReadTailWireClasses(TomlTable root)
    {
        if (!root.TryGetValue("tail_wire_class", out var v) || v is not TomlTableArray arr || arr.Count == 0)
            return null;
        var list = new List<TailWireClassToml>(arr.Count);
        foreach (var x in arr)
        {
            if (x is not TomlTable t)
                continue;
            list.Add(new TailWireClassToml
            {
                Id = GetString(t, "id"),
                Kind = GetString(t, "kind"),
                BetweenSlotsAnyOf = GetStringArray(t, "between_slots_any_of"),
            });
        }
        return list.Count == 0 ? null : list;
    }

    private static int? GetInt(TomlTable t, string key) =>
        t.TryGetValue(key, out var v) && v is long l ? (int)l : null;

    private static bool? GetBool(TomlTable t, string key) =>
        t.TryGetValue(key, out var v) && v is bool b ? b : null;

    private static string? GetString(TomlTable t, string key) =>
        t.TryGetValue(key, out var v) && v is string s ? s : null;

    private static string[]? GetStringArray(TomlTable t, string key)
    {
        if (!t.TryGetValue(key, out var v) || v is not TomlArray a || a.Count == 0)
            return null;
        var list = new List<string>(a.Count);
        foreach (var x in a)
        {
            if (x is string s && !string.IsNullOrWhiteSpace(s))
                list.Add(s);
        }
        return list.Count == 0 ? null : list.ToArray();
    }

    private static Dictionary<string, string>? GetStringMap(TomlTable t, string key)
    {
        if (!t.TryGetValue(key, out var v) || v is not TomlTable map)
            return null;
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in map)
        {
            if (kv.Value is string s)
                d[kv.Key] = s;
        }
        return d.Count == 0 ? null : d;
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
        Build(ParseIntentCatalogToRoot(tomlTrimmed, "tests"));

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
