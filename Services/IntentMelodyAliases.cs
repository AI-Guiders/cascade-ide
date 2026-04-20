namespace CascadeIDE.Services;

/// <summary>
/// <b>Command Melody</b> (<c>c:</c>) — alias → <see cref="IdeCommands"/> id.
/// Источник: <see cref="BundledRelativePath"/> (файл рядом с exe, иначе EmbeddedResource), см. <c>docs/intent-melody-language-v1.md</c>, ADR 0060 §11.
/// </summary>
public static class IntentMelodyAliases
{
    /// <summary>Относительно <see cref="AppContext.BaseDirectory"/>; оверлей поверх встроенного TOML.</summary>
    public const string BundledRelativePath = "IntentMelody/intent-melody-aliases.toml";

    private static readonly Lazy<Dictionary<string, string>> AliasToCommandIdLazy = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    private sealed class IntentMelodyAliasesTomlRoot
    {
        public Dictionary<string, string>? Aliases { get; set; }
    }

    private static Dictionary<string, string> Load()
    {
        if (!BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var text) || string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(
                $"Missing {BundledRelativePath} (file under AppContext.BaseDirectory or embedded resource in CascadeIDE assembly).");

        var root = CascadeTomlSerializer.Deserialize<IntentMelodyAliasesTomlRoot>(text.Trim());
        if (root?.Aliases is not { Count: > 0 })
            throw new InvalidOperationException($"Invalid {BundledRelativePath}: expected [aliases] with at least one entry.");

        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in root.Aliases)
        {
            var key = kv.Key?.Trim();
            var val = kv.Value?.Trim();
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(val))
                continue;
            d[key.ToLowerInvariant()] = val;
        }

        if (d.Count == 0)
            throw new InvalidOperationException($"No valid alias entries in {BundledRelativePath}.");

        return d;
    }

    private static Dictionary<string, string> AliasToCommandId => AliasToCommandIdLazy.Value;

    /// <summary>Строка палитры начинается с <c>c:</c> (регистр первой буквы допускается).</summary>
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

    /// <summary>Все пары (alias, command_id) по возрастанию alias.</summary>
    public static IReadOnlyList<(string Alias, string CommandId)> AllPairs() =>
        AliasToCommandId
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();

    /// <summary>
    /// Первые <paramref name="maxAliases"/> alias (по имени) для плейсхолдера палитры и подсказки «c: melody (…)»;
    /// если alias больше — добавляется «…».
    /// </summary>
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

    /// <summary>Alias, чей хвост начинается с <paramref name="tailNormalized"/> (включая пустой — всё).</summary>
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

    public static string? TryResolveExactCommandId(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
            return null;
        return AliasToCommandId.TryGetValue(tailNormalized, out var id) ? id : null;
    }

    /// <summary>
    /// Есть ли alias длиннее <paramref name="tailNormalized"/>, который начинается с этого хвоста (например <c>gs</c> vs <c>gsu</c>).
    /// Нужно для аккорда без Enter: не исполнять точное совпадение, пока возможно продолжение.
    /// </summary>
    public static bool HasStrictLongerAliasPrefix(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
            return false;
        foreach (var kv in AliasToCommandId.Keys)
        {
            if (kv.Length > tailNormalized.Length && kv.StartsWith(tailNormalized, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
