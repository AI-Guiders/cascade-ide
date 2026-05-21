using Tomlyn;
using Tomlyn.Model;

namespace CascadeIDE.Services;

/// <summary>Глубокий merge TOML-документов: <paramref name="overlay"/> перекрывает <paramref name="baseToml"/> по ключам таблиц.</summary>
internal static class TomlTableMerge
{
    public static string MergeTomlDocuments(string baseToml, string overlayToml)
    {
        var baseTable = TomlSerializer.Deserialize<TomlTable>(baseToml, CascadeTomlSerializer.Options)
            ?? throw new InvalidOperationException("Base TOML did not deserialize to TomlTable.");
        var overlayTable = TomlSerializer.Deserialize<TomlTable>(overlayToml, CascadeTomlSerializer.Options)
            ?? throw new InvalidOperationException("Overlay TOML did not deserialize to TomlTable.");
        MergeInto(baseTable, overlayTable);
        return TomlSerializer.Serialize(baseTable, CascadeTomlSerializer.Options);
    }

    private static void MergeInto(TomlTable target, TomlTable overlay)
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
