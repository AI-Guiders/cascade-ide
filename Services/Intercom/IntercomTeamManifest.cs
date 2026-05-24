namespace CascadeIDE.Services.Intercom;

/// <summary>Содержимое <c>.cascade-ide/intercom-team.toml</c> (ADR 0144 §2.1).</summary>
public sealed class IntercomTeamManifest
{
    public IntercomTeamManifestSection Team { get; set; } = new();

    public string TeamId => Team.TeamId;

    public string DisplayName => string.IsNullOrWhiteSpace(Team.DisplayName) ? Team.TeamId : Team.DisplayName;
}

public sealed class IntercomTeamManifestSection
{
    public string TeamId { get; set; } = "";

    public string DisplayName { get; set; } = "";
}

/// <summary>Поиск manifest от workspace root вверх к git root.</summary>
public static class IntercomTeamManifestResolver
{
    public const string RelativeManifestPath = ".cascade-ide/intercom-team.toml";

    public static IntercomTeamManifest? TryResolve(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        var dir = new DirectoryInfo(Path.GetFullPath(workspaceRoot));
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, RelativeManifestPath);
            if (File.Exists(path))
                return TryParseFile(path);

            var git = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git))
                break;

            dir = dir.Parent;
        }

        return null;
    }

    public static IntercomTeamManifest? TryParseFile(string path)
    {
        try
        {
            var toml = File.ReadAllText(path);
            var manifest = CascadeTomlSerializer.Deserialize<IntercomTeamManifest>(toml);
            if (manifest is null || string.IsNullOrWhiteSpace(manifest.TeamId))
                return null;
            return manifest;
        }
        catch
        {
            return null;
        }
    }
}
