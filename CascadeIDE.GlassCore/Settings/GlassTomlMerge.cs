#nullable enable
using Tomlyn;
using Tomlyn.Model;

namespace CascadeIDE.GlassCore.Settings;

/// <summary>Deep TOML table merge — same idea as CIDE <c>TomlTableMerge</c> (defaults ← overlay).</summary>
public static class GlassTomlMerge
{
    public static string MergeDocuments(string baseToml, string overlayToml)
    {
        var baseTable = TomlSerializer.Deserialize<TomlTable>(baseToml)
            ?? throw new InvalidOperationException("Base TOML did not deserialize to TomlTable.");
        var overlayTable = TomlSerializer.Deserialize<TomlTable>(overlayToml)
            ?? throw new InvalidOperationException("Overlay TOML did not deserialize to TomlTable.");
        MergeInto(baseTable, overlayTable);
        return TomlSerializer.Serialize(baseTable);
    }

    static void MergeInto(TomlTable target, TomlTable overlay)
    {
        foreach (var (key, value) in overlay)
        {
            if (value is TomlTable overlayChild
                && target.TryGetValue(key, out var existing)
                && existing is TomlTable targetChild)
            {
                MergeInto(targetChild, overlayChild);
                continue;
            }

            target[key] = value;
        }
    }
}
