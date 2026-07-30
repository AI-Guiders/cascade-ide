#nullable enable
using CascadeIDE.Features.Settings.DataAcquisition;
using Tomlyn;
using Tomlyn.Model;

namespace CascadeIDE.GlassCore.Settings;

/// <summary>
/// Thin peel of CascadeIDE settings for operator glass hosts.
/// Merge: <c>defaults-settings.toml</c> → optional <c>.cascade/workspace.toml</c> → user <c>settings.toml</c>.
/// Same paths/keys as Avalonia CIDE — not a second SSOT.
/// </summary>
public sealed class IdeGlassSettings
{
    public string Topology { get; init; } = "(F)";
    public string Tier { get; init; } = "auto";
    public string PrimaryWorkSurface { get; init; } = "intercom";
    public PresentationGrammarSlice Grammar { get; init; } = PresentationGrammarSlice.Default;

    public string SettingsPath { get; init; } = "";
    public string? WorkspaceTomlPath { get; init; }
    public string? DefaultsPath { get; init; }
    public string? WorkspaceRoot { get; init; }

    public static string DefaultSettingsPath => UserSettingsPaths.GetSettingsFilePath();

    /// <param name="settingsPath">User settings.toml; default LocalAppData.</param>
    /// <param name="workspaceRoot">Repo root with <c>.cascade/workspace.toml</c>; null = try discover from cwd.</param>
    /// <param name="defaultsPath">Optional explicit defaults-settings.toml; else BaseDirectory / embedded / walk-up.</param>
    public static IdeGlassSettings Load(
        string? settingsPath = null,
        string? workspaceRoot = null,
        string? defaultsPath = null)
    {
        var userPath = string.IsNullOrWhiteSpace(settingsPath) ? DefaultSettingsPath : settingsPath;
        var root = string.IsNullOrWhiteSpace(workspaceRoot)
            ? TryDiscoverWorkspaceRoot()
            : workspaceRoot.Trim();
        var workspacePath = root is null ? null : Path.Combine(root, ".cascade", "workspace.toml");

        var defaultsText = TryReadDefaultsToml(defaultsPath, out var resolvedDefaults);
        var merged = defaultsText ?? "";

        if (workspacePath is not null && File.Exists(workspacePath))
        {
            var workspaceText = File.ReadAllText(workspacePath);
            merged = string.IsNullOrWhiteSpace(merged)
                ? workspaceText
                : GlassTomlMerge.MergeDocuments(merged, workspaceText);
        }

        if (File.Exists(userPath))
        {
            var userText = File.ReadAllText(userPath);
            merged = string.IsNullOrWhiteSpace(merged)
                ? userText
                : GlassTomlMerge.MergeDocuments(merged, userText);
        }

        if (string.IsNullOrWhiteSpace(merged))
        {
            return new IdeGlassSettings
            {
                SettingsPath = userPath,
                WorkspaceTomlPath = workspacePath is not null && File.Exists(workspacePath) ? workspacePath : null,
                DefaultsPath = resolvedDefaults,
                WorkspaceRoot = root,
            };
        }

        var model = TomlSerializer.Deserialize<TomlTable>(merged) ?? new TomlTable();
        return FromTable(model, userPath, workspacePath, resolvedDefaults, root);
    }

    static IdeGlassSettings FromTable(
        TomlTable model,
        string settingsPath,
        string? workspacePath,
        string? defaultsPath,
        string? workspaceRoot)
    {
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
            WorkspaceTomlPath = workspacePath is not null && File.Exists(workspacePath) ? workspacePath : null,
            DefaultsPath = defaultsPath,
            WorkspaceRoot = workspaceRoot,
        };
    }

    static string? TryReadDefaultsToml(string? explicitPath, out string? resolvedPath)
    {
        resolvedPath = null;

        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
        {
            resolvedPath = explicitPath;
            return File.ReadAllText(explicitPath);
        }

        var underBase = Path.Combine(AppContext.BaseDirectory, "Settings", "defaults-settings.toml");
        if (File.Exists(underBase))
        {
            resolvedPath = underBase;
            return File.ReadAllText(underBase);
        }

        var walked = WalkUpForDefaults();
        if (walked is not null)
        {
            resolvedPath = walked;
            return File.ReadAllText(walked);
        }

        var embedded = TryReadEmbeddedDefaults();
        if (embedded is not null)
        {
            resolvedPath = "embedded:Settings/defaults-settings.toml";
            return embedded;
        }

        return null;
    }

    static string? WalkUpForDefaults()
    {
        try
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "Settings", "defaults-settings.toml");
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        catch
        {
            // ignore discovery failures
        }

        return null;
    }

    static string? TryReadEmbeddedDefaults()
    {
        var asm = typeof(IdeGlassSettings).Assembly;
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (!name.EndsWith("defaults-settings.toml", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("defaults-settings.toml", StringComparison.OrdinalIgnoreCase))
                continue;

            using var stream = asm.GetManifestResourceStream(name);
            if (stream is null)
                continue;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        return null;
    }

    static string? TryDiscoverWorkspaceRoot()
    {
        try
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            for (var i = 0; i < 10 && dir is not null; i++, dir = dir.Parent)
            {
                var cascade = Path.Combine(dir.FullName, ".cascade", "workspace.toml");
                if (File.Exists(cascade))
                    return dir.FullName;
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                    return dir.FullName;
            }
        }
        catch
        {
            // ignore
        }

        return null;
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
