#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CascadeIDE.Services;
using Tomlyn;
using Tomlyn.Model;

namespace CascadeIDE.Intercom;

/// <summary>
/// Thin Avalonia-free peel of CIDE <c>IntentMelody/intent-catalog.toml</c> for Glass Ctrl+Q <c>c:</c>.
/// Full slash/parametric stack stays in host <see cref="Services.IntentMelodyAliases"/>.
/// </summary>
public static class GlassIntentMelodyCatalog
{
    public const string BundledRelativePath = "IntentMelody/intent-catalog.toml";
    public const string MelodyRowIdPrefix = "melody:";

    static readonly Lazy<IReadOnlyList<GlassMelodyAlias>> LazyAliases =
        new(LoadAliases, LazyThreadSafetyMode.ExecutionAndPublication);

    public sealed record GlassMelodyAlias(string Alias, string CommandId, string Help);

    public static IReadOnlyList<GlassMelodyAlias> All() => LazyAliases.Value;

    public static string SampleAliases(int maxAliases = 6)
    {
        if (maxAliases <= 0)
            return "";

        var all = All();
        if (all.Count == 0)
            return "";

        var take = Math.Min(maxAliases, all.Count);
        var s = string.Join(", ", all.Take(take).Select(a => "c:" + a.Alias));
        if (all.Count > maxAliases)
            s += ", …";
        return s;
    }

    public static IReadOnlyList<GlassMelodyAlias> FilterByTailPrefix(string tailNormalized)
    {
        var all = All();
        var aliasPrefix = GlassMelodyTail.AliasPrefix(tailNormalized);
        if (string.IsNullOrEmpty(aliasPrefix))
            return all;

        return all
            .Where(a => a.Alias.StartsWith(aliasPrefix, StringComparison.Ordinal))
            .ToArray();
    }

    public static bool IsMelodyDiscoverabilityRow(string id) =>
        id.StartsWith(MelodyRowIdPrefix, StringComparison.Ordinal);

    public static string ToRowId(string commandId) => MelodyRowIdPrefix + commandId;

    public static bool TryParseRowId(string? rowId, out string commandId)
    {
        commandId = "";
        if (string.IsNullOrWhiteSpace(rowId)
            || !rowId.StartsWith(MelodyRowIdPrefix, StringComparison.Ordinal))
            return false;

        commandId = rowId[MelodyRowIdPrefix.Length..].Trim();
        return commandId.Length > 0;
    }

#if DEBUG
    internal static void ResetForTests() { /* lazy fixed — reload needs process restart; tests use live embed */ }
#endif

    static IReadOnlyList<GlassMelodyAlias> LoadAliases()
    {
        if (!TryReadCatalogText(out var text) || string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<GlassMelodyAlias>();
        }

        var root = TomlSerializer.Deserialize<TomlTable>(text.Trim(), CascadeTomlSerializer.Options);
        if (root is null || !root.TryGetValue("command", out var cmdVal) || cmdVal is not TomlTableArray commands)
            return Array.Empty<GlassMelodyAlias>();

        var map = new Dictionary<string, GlassMelodyAlias>(StringComparer.Ordinal);
        foreach (var item in commands)
        {
            if (item is not TomlTable t)
                continue;

            var slug = GetString(t, "melody_slug");
            var commandId = GetString(t, "command_id");
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(commandId))
                continue;

            if (GetBool(t, "enabled") == false)
                continue;

            var alias = slug.Trim().ToLowerInvariant();
            if (alias.Length == 0 || map.ContainsKey(alias))
                continue;

            var help = GetString(t, "melody_palette_usage_hint")
                       ?? FirstSlashHelp(t)
                       ?? commandId.Trim();

            map[alias] = new GlassMelodyAlias(alias, commandId.Trim(), help.Trim());
        }

        return map.Values
            .OrderBy(a => a.Alias, StringComparer.Ordinal)
            .ToArray();
    }

    static bool TryReadCatalogText([NotNullWhen(true)] out string? text)
    {
        text = null;
        var disk = Path.Combine(
            AppContext.BaseDirectory,
            BundledRelativePath.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            if (File.Exists(disk))
            {
                text = File.ReadAllText(disk);
                return true;
            }
        }
        catch
        {
            // fall through to embedded
        }

        var asm = typeof(GlassIntentMelodyCatalog).Assembly;
        var name = "CascadeIDE.IntentMelody.intent-catalog.toml";
        using var stream = asm.GetManifestResourceStream(name);
        if (stream is null)
        {
            // SDK may use GlassCore assembly name prefix when RootNamespace differs from AssemblyName.
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (n.EndsWith("IntentMelody.intent-catalog.toml", StringComparison.Ordinal))
                {
                    using var s2 = asm.GetManifestResourceStream(n);
                    if (s2 is null)
                        continue;
                    using var r2 = new StreamReader(s2);
                    text = r2.ReadToEnd();
                    return !string.IsNullOrWhiteSpace(text);
                }
            }

            return false;
        }

        using var reader = new StreamReader(stream);
        text = reader.ReadToEnd();
        return !string.IsNullOrWhiteSpace(text);
    }

    static string? FirstSlashHelp(TomlTable command)
    {
        if (!command.TryGetValue("slash", out var slashVal))
            return null;

        if (slashVal is TomlTableArray arr)
        {
            foreach (var x in arr)
            {
                if (x is TomlTable st)
                {
                    var help = GetString(st, "help");
                    if (!string.IsNullOrWhiteSpace(help))
                        return help;
                }
            }
        }
        else if (slashVal is TomlTable single)
        {
            return GetString(single, "help");
        }

        return null;
    }

    static string? GetString(TomlTable t, string key)
    {
        if (!t.TryGetValue(key, out var v) || v is null)
            return null;
        return v as string ?? v.ToString();
    }

    static bool? GetBool(TomlTable t, string key)
    {
        if (!t.TryGetValue(key, out var v) || v is null)
            return null;
        return v switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var p) => p,
            _ => null,
        };
    }
}
