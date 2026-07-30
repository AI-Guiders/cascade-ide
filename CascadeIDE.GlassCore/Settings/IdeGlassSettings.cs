#nullable enable
using Tomlyn;
using Tomlyn.Model;

namespace CascadeIDE.GlassCore.Settings;

/// <summary>
/// Thin peel of CascadeIDE settings.toml for operator glass hosts.
/// Same path + keys as Avalonia CIDE — not a second SSOT.
/// Full <c>SettingsService</c>/<c>CascadeIdeSettings</c> extract comes later (OutWit graph).
/// </summary>
public sealed class IdeGlassSettings
{
    public string Topology { get; init; } = "(F)";
    public string Tier { get; init; } = "auto";
    public string PrimaryWorkSurface { get; init; } = "intercom";
    public PresentationGrammarSlice Grammar { get; init; } = PresentationGrammarSlice.Default;

    public string SettingsPath { get; init; } = "";

    public static string DefaultSettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CascadeIDE",
            "settings.toml");

    public static IdeGlassSettings Load(string? path = null)
    {
        var settingsPath = string.IsNullOrWhiteSpace(path) ? DefaultSettingsPath : path;
        if (!File.Exists(settingsPath))
        {
            return new IdeGlassSettings { SettingsPath = settingsPath };
        }

        var text = File.ReadAllText(settingsPath);
        var model = TomlSerializer.Deserialize<TomlTable>(text)
            ?? new TomlTable();

        var topology = GetString(model, "display", "screens", "topology") ?? "(F)";
        var tier = GetString(model, "display", "presentation", "tier") ?? "auto";
        var surface = GetString(model, "workspace", "primary_work_surface") ?? "intercom";

        var grammar = PresentationGrammarSlice.Default with
        {
            Brackets = GetString(model, "display", "screens", "grammar", "brackets") ?? "()",
            BetweenScreens = GetString(model, "display", "screens", "grammar", "between_screens") ?? " ",
            BetweenZones = GetString(model, "display", "screens", "grammar", "between_zones") ?? "+",
            Pfd = GetString(model, "display", "screens", "grammar", "pfd") ?? "P",
            Forward = GetString(model, "display", "screens", "grammar", "forward") ?? "F",
            Mfd = GetString(model, "display", "screens", "grammar", "mfd") ?? "M",
        };

        return new IdeGlassSettings
        {
            Topology = topology.Trim(),
            Tier = tier.Trim(),
            PrimaryWorkSurface = surface.Trim().ToLowerInvariant(),
            Grammar = grammar,
            SettingsPath = settingsPath,
        };
    }

    static string? GetString(TomlTable root, params string[] path)
    {
        object current = root;
        foreach (var key in path)
        {
            if (current is not TomlTable table || !table.TryGetValue(key, out var next) || next is null)
                return null;
            current = next;
        }

        return current switch
        {
            string s => s,
            _ => current.ToString()
        };
    }
}

public readonly record struct PresentationGrammarSlice(
    string Brackets,
    string BetweenScreens,
    string BetweenZones,
    string Pfd,
    string Forward,
    string Mfd)
{
    public static PresentationGrammarSlice Default => new("()", " ", "+", "P", "F", "M");
}
