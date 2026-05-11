namespace CascadeIDE.Services;

/// <summary>
/// <b>Command Melody</b> (<c>c:</c>) — slug → <see cref="IdeCommands"/> id и каталог ADR 0109 (<see cref="IntentMelodyCatalog"/>).
/// Источник: <see cref="BundledRelativePath"/> (файл рядом с exe, иначе EmbeddedResource), см. <c>docs/intent-melody-language-v1.md</c>, ADR 0060 §11, ADR 0109.
/// </summary>
public static class IntentMelodyAliases
{
    /// <summary>Относительно <see cref="AppContext.BaseDirectory"/>; оверлей поверх встроенного TOML.</summary>
    public const string BundledRelativePath = "IntentMelody/intent-melody-aliases.toml";

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
        public int? MelodyCatalogSchemaVersion { get; set; }
        public Dictionary<string, string>? Aliases { get; set; }
        public List<MelodyRootToml>? MelodyRoot { get; set; }
        public List<TailWireClassToml>? TailWireClass { get; set; }
    }

    /// <remarks>TOML: <c>[[melody_root]]</c>; см. ADR 0109.</remarks>
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

    /// <remarks>Поля читает Tomlyn; реестр используется при расширении разборщика wire (ADR 0109).</remarks>
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
        if (!BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var text) || string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(
                $"Missing {BundledRelativePath} (file under AppContext.BaseDirectory or embedded resource in CascadeIDE assembly).");

        var root = CascadeTomlSerializer.Deserialize<IntentMelodyTomlRoot>(text.Trim())
                   ?? throw new InvalidOperationException($"Invalid {BundledRelativePath}: empty parse.");

        return Build(root);
    }

    internal static IntentMelodyBundleState Build(IntentMelodyTomlRoot root)
    {
        var wireClasses = LoadTailWireClassTable(root);

        var normAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in root.Aliases ?? new Dictionary<string, string>())
        {
            var key = kv.Key?.Trim().ToLowerInvariant();
            var val = kv.Value?.Trim();
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(val))
                continue;
            normAliases[key] = val;
        }

        Dictionary<string, MelodyRootEntry> merged = new(StringComparer.OrdinalIgnoreCase);

        static string? NormOptional(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        static IntentMelodyShape ShapeFromRow(MelodyRootToml row) =>
            string.IsNullOrWhiteSpace(row.Shape)
                ? (string.IsNullOrWhiteSpace(row.TailSignature)
                    ? IntentMelodyShape.Simple
                    : IntentMelodyShape.Parametric)
                : ParseShapeMandatory(row.Shape!);

        static IntentMelodyShape ParseShapeMandatory(string raw)
        {
            var x = raw.Trim();
            if (string.Equals(x, "simple", StringComparison.OrdinalIgnoreCase))
                return IntentMelodyShape.Simple;
            if (string.Equals(x, "parametric", StringComparison.OrdinalIgnoreCase))
                return IntentMelodyShape.Parametric;
            throw new InvalidOperationException($"{BundledRelativePath}: unknown shape '{raw}'.");
        }

        /// <summary>По умолчанию для параметрики с двумя int-слотами без URL (как els/eld): показывать подсказку при «голом» slug в палитре.</summary>
        static bool InferShowUsageHintIfBareSlug(IntentMelodyShape shape, string? tailSignatureRaw)
        {
            if (shape != IntentMelodyShape.Parametric)
                return false;
            var ts = tailSignatureRaw?.Trim();
            if (string.IsNullOrEmpty(ts))
                return false;
            if (IntentMelodyTailSemantics.HasUrlSlot(ts))
                return false;
            return IntentMelodyTailSemantics.CountDelimitedNumericSlots(ts) >= 2;
        }

        if (root.MelodyRoot is { Count: > 0 })
        {
            foreach (var row in root.MelodyRoot!)
            {
                var slug = row.Slug?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(slug))
                    continue;

                if (merged.ContainsKey(slug))
                    throw new InvalidOperationException(
                        $"{BundledRelativePath}: duplicate [[melody_root]] slug '{slug}'.");

                var cmdId = row.CommandId?.Trim() ?? "";
                var shape = ShapeFromRow(row);
                var showUsageHintIfBareSlug = row.ShowUsageHintIfBareSlug ?? InferShowUsageHintIfBareSlug(shape, row.TailSignature);

                merged[slug] = new MelodyRootEntry(
                    slug,
                    cmdId,
                    shape,
                    showUsageHintIfBareSlug,
                    NormOptional(row.TailSignature),
                    NormOptional(row.WireClass),
                    NormOptional(row.ChordCommit),
                    NormOptional(row.PaletteHintSlug),
                    NormOptional(row.PaletteUsageHint),
                    NormOptional(row.PaletteUsageCategory));
            }

            foreach (var kv in normAliases)
            {
                if (merged.ContainsKey(kv.Key))
                    continue;

                merged[kv.Key] = new MelodyRootEntry(kv.Key, kv.Value, IntentMelodyShape.Simple, ShowUsageHintIfBareSlug: false,
                    null, null, null, null);
            }
        }
        else
        {
            foreach (var kv in normAliases)
            {
                merged[kv.Key] = new MelodyRootEntry(kv.Key, kv.Value, IntentMelodyShape.Simple, ShowUsageHintIfBareSlug: false,
                    null, null, null, null);
            }
        }

        foreach (var e in merged.Values.OrderBy(e => e.Slug, StringComparer.Ordinal))
            ValidateChordCommitField(e.Slug, e.ChordCommit, e.Shape);

        ValidateParametricWireBindings(merged, wireClasses);

        var commandMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in merged.Values.OrderBy(e => e.Slug, StringComparer.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(e.CommandId))
                commandMap[e.Slug] = e.CommandId;
        }

        if (commandMap.Count == 0)
            throw new InvalidOperationException($"{BundledRelativePath}: нет ни одной пары slug→command_id.");

        return new IntentMelodyBundleState(commandMap, new IntentMelodyCatalogSnapshot(merged, wireClasses));

        static Dictionary<string, TailWireClassEntry> LoadTailWireClassTable(IntentMelodyTomlRoot tomlRoot)
        {
            var tables = new Dictionary<string, TailWireClassEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in tomlRoot.TailWireClass ?? [])
            {
                var idNorm = row.Id?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(idNorm))
                    continue;
                if (tables.ContainsKey(idNorm))
                    throw new InvalidOperationException(
                        $"{BundledRelativePath}: duplicate [[tail_wire_class]] id '{idNorm}'.");

                var kindRaw = row.Kind ?? "";
                var kind =
                    kindRaw.Trim().ToLowerInvariant() switch
                    {
                        "single_remainder" => TailWireKind.SingleRemainder,
                        "delimited_slots" => TailWireKind.DelimitedSlots,
                        _ => throw new InvalidOperationException(
                            $"{BundledRelativePath}: [[tail_wire_class]] '{idNorm}' unknown kind '{row.Kind}'.")
                    };

                var seps =
                    NormalizeBetweenSlots(kind, row.BetweenSlotsAnyOf ?? [], idNorm);
                tables[idNorm] = new TailWireClassEntry(idNorm, kind, seps);
            }

            return tables;

            static string[] NormalizeBetweenSlots(TailWireKind kind, string[] rawArr, string idNorm)
            {
                if (kind == TailWireKind.SingleRemainder)
                    return [];

                if (rawArr.Length == 0)
                    throw new InvalidOperationException(
                        $"{BundledRelativePath}: [[tail_wire_class]] '{idNorm}' kind '{nameof(TailWireKind.DelimitedSlots)}' requires between_slots_any_of.");

                List<string> acc = [];
                foreach (var s in rawArr)
                {
                    if (string.IsNullOrEmpty(s))
                        throw new InvalidOperationException(
                            $"{BundledRelativePath}: [[tail_wire_class]] '{idNorm}' empty separator entry.");
                    if (s.Length != 1)
                        throw new InvalidOperationException(
                            $"{BundledRelativePath}: [[tail_wire_class]] '{idNorm}' separator '{s}' must be a single character.");
                    acc.Add(s);
                }

                acc.Sort(StringComparer.Ordinal);
                for (var i = 1; i < acc.Count; i++)
                {
                    if (!string.Equals(acc[i], acc[i - 1], StringComparison.Ordinal))
                        continue;
                    acc.RemoveAt(i);
                    i--;
                }

                return acc.ToArray();
            }
        }

        static void ValidateChordCommitField(string slug, string? chordCommit, IntentMelodyShape shape)
        {
            if (string.IsNullOrWhiteSpace(chordCommit))
                return;
            var cc = chordCommit.Trim().ToLowerInvariant();
            if (cc != "enter" && cc != "immediate" && cc != "instant")
            {
                throw new InvalidOperationException(
                    $"{BundledRelativePath}: [[melody_root]] '{slug}' unknown chord_commit '{chordCommit}' (allowed: enter, immediate, instant).");
            }

            if (shape == IntentMelodyShape.Simple && cc != "enter")
            {
                throw new InvalidOperationException(
                    $"{BundledRelativePath}: [[melody_root]] '{slug}' shape simple — chord_commit должен быть пустым или enter.");
            }
        }

        static void ValidateParametricWireBindings(
            IReadOnlyDictionary<string, MelodyRootEntry> roots,
            Dictionary<string, TailWireClassEntry> wireClasses)
        {
            foreach (var e in roots.Values.OrderBy(x => x.Slug, StringComparer.Ordinal))
            {
                if (e.Shape != IntentMelodyShape.Parametric)
                    continue;

                var hasTail = !string.IsNullOrWhiteSpace(e.TailSignature);
                var wireRef = NormOptionalWireClass(e.WireClass);
                if (hasTail && wireRef is null)
                {
                    throw new InvalidOperationException(
                        $"{BundledRelativePath}: parametric [[melody_root]] '{e.Slug}' с tail_signature требует поле wire_class.");
                }

                if (wireRef is null)
                    continue;

                if (!wireClasses.TryGetValue(wireRef, out var row))
                    throw new InvalidOperationException(
                        $"{BundledRelativePath}: unknown wire_class '{e.WireClass}' for melody_root slug '{e.Slug}'.");

                IntentMelodyTailSemantics.ValidateMelodyAgainstWireClass(e, row);
            }
        }

        static string? NormOptionalWireClass(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();
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
