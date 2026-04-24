#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CascadeIDE.Services;

/// <summary>
/// <c>.cascade-ide/launch-profiles.toml</c> — каталог launch profiles (ADR 0090), миграция с <c>startup-project.json</c>.
/// </summary>
public static class LaunchProfilesStore
{
    public const string FileName = "launch-profiles.toml";
    public const string DefaultProfileId = "Default";
    public const string DefaultConfiguration = "Debug";

    public static string GetStorePath(string solutionPath)
    {
        var root = BreakpointsFileService.GetWorkspaceRoot(solutionPath);
        if (string.IsNullOrEmpty(root))
            return "";
        return Path.Combine(root, ".cascade-ide", FileName);
    }

    public static void Delete(string solutionPath)
    {
        var p = GetStorePath(solutionPath);
        if (string.IsNullOrEmpty(p) || !File.Exists(p))
            return;
        try { File.Delete(p); }
        catch { /* ignore */ }
    }

    /// <summary>Активный стартовый проект: отн. путь к .csproj от корня решения.</summary>
    public static bool TryGetActiveProjectRelativePath(
        string solutionPath,
        [NotNullWhen(true)] out string? projectRelative,
        [NotNullWhen(false)] out string? error)
    {
        projectRelative = null;
        error = null;
        if (!TryLoadDocument(solutionPath, out var doc, out var err) || doc is null)
        {
            error = string.IsNullOrEmpty(err) ? "launch_profiles_unavailable" : err;
            return false;
        }

        if (string.IsNullOrWhiteSpace(doc.ActiveProfile))
        {
            error = "active_profile_missing";
            return false;
        }

        var id = doc.ActiveProfile.Trim();
        if (doc.Profiles is null || !doc.Profiles.TryGetValue(id, out var p) || p is null)
        {
            error = "profile_not_found: " + id;
            return false;
        }

        if (string.IsNullOrWhiteSpace(p.Project))
        {
            error = "profile_target_unresolved: empty project in profile " + id;
            return false;
        }

        projectRelative = p.Project!.Trim().Replace('/', Path.DirectorySeparatorChar);
        return true;
    }

    /// <summary>Разрешение профиля для запуска: <paramref name="profileName"/> или активный, если <c>null</c>/пусто.</summary>
    public static bool TryResolveProfileForLaunch(
        string solutionPath,
        string? profileName,
        out LaunchProfileData data,
        [NotNullWhen(false)] out string? error)
    {
        data = default;
        error = null;
        if (!TryLoadDocument(solutionPath, out var doc, out var err) || doc is null)
        {
            error = string.IsNullOrEmpty(err) ? "launch_profiles_unavailable" : err;
            return false;
        }

        var id = string.IsNullOrWhiteSpace(profileName) ? doc.ActiveProfile : profileName;
        if (string.IsNullOrWhiteSpace(id))
        {
            error = "active_profile_missing";
            return false;
        }

        id = id.Trim();
        if (doc.Profiles is null || !doc.Profiles.TryGetValue(id, out var p) || p is null)
        {
            error = "profile_not_found: " + id;
            return false;
        }

        if (string.IsNullOrWhiteSpace(p.Project))
        {
            error = "profile_target_unresolved: empty project in profile " + id;
            return false;
        }

        var rel = p.Project!.Trim().Replace('/', Path.DirectorySeparatorChar);
        var config = string.IsNullOrWhiteSpace(p.Configuration) ? DefaultConfiguration : p.Configuration!.Trim();
        var programArgs = p.ProgramArgs is { Count: > 0 } ? p.ProgramArgs : null;
        var cwdRel = string.IsNullOrWhiteSpace(p.WorkingDirectory) ? null : p.WorkingDirectory!.Trim().Replace('/', Path.DirectorySeparatorChar);

        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (p.Env is not null)
        {
            foreach (var (k, v) in p.Env)
            {
                if (string.IsNullOrEmpty(k) || v is null)
                    continue;
                env[k] = v;
            }
        }

        if (!string.IsNullOrWhiteSpace(p.ApplicationUrls) &&
            !env.ContainsKey("ASPNETCORE_URLS") &&
            !env.ContainsKey("ASPNETCORE__URLS"))
        {
            env["ASPNETCORE_URLS"] = p.ApplicationUrls!.Trim();
        }

        var openBrowser = p.LaunchBrowser == true;
        var launchUrl = string.IsNullOrWhiteSpace(p.LaunchUrl) ? null : p.LaunchUrl.Trim();
        data = new LaunchProfileData(id, rel, config, programArgs, cwdRel, env, openBrowser, launchUrl);
        return true;
    }

    /// <summary>Установить <c>active_profile</c> в TOML (после проверки существования профиля).</summary>
    public static bool TrySetActiveProfile(
        string solutionPath,
        string profileId,
        [NotNullWhen(false)] out string? error)
    {
        error = null;
        var id = profileId.Trim();
        if (string.IsNullOrEmpty(id))
        {
            error = "empty_profile_id";
            return false;
        }

        if (!TryLoadDocument(solutionPath, out var doc, out var err) || doc is null)
        {
            error = string.IsNullOrEmpty(err) ? "launch_profiles_unavailable" : err;
            return false;
        }

        if (doc.Profiles is null || !doc.Profiles.ContainsKey(id))
        {
            error = "profile_not_found: " + id;
            return false;
        }

        doc.ActiveProfile = id;
        WriteDocument(solutionPath, doc);
        return true;
    }

    /// <summary>Текущий <c>active_profile</c> из TOML (для селектора в UI).</summary>
    public static bool TryGetActiveProfileName(
        string solutionPath,
        [NotNullWhen(true)] out string? name,
        [NotNullWhen(false)] out string? error)
    {
        name = null;
        error = null;
        if (!TryLoadDocument(solutionPath, out var doc, out var err) || doc is null)
        {
            error = string.IsNullOrEmpty(err) ? "launch_profiles_unavailable" : err;
            return false;
        }

        if (string.IsNullOrWhiteSpace(doc.ActiveProfile))
        {
            error = "active_profile_missing";
            return false;
        }

        name = doc.ActiveProfile!.Trim();
        return true;
    }

    /// <summary>Имена профилей для UI (стабильный порядок).</summary>
    public static bool TryGetOrderedProfileIds(
        string solutionPath,
        [NotNullWhen(true)] out IReadOnlyList<string>? names,
        [NotNullWhen(false)] out string? error)
    {
        names = null;
        error = null;
        if (!TryLoadDocument(solutionPath, out var doc, out var err) || doc is null)
        {
            error = string.IsNullOrEmpty(err) ? "launch_profiles_unavailable" : err;
            return false;
        }

        if (doc.Profiles is null || doc.Profiles.Count == 0)
        {
            error = "no_profiles";
            return false;
        }

        names = doc.Profiles.Keys
            .OrderBy(static s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return true;
    }

    /// <summary>Импорт из <c>Properties/launchSettings.json</c> выбранного проекта (отн. путь к .csproj от корня решения).</summary>
    public static bool TryImportFromLaunchSettings(
        string solutionPath,
        string projectPathRelativeToSolution,
        out int profilesWritten,
        [NotNullWhen(false)] out string? error)
    {
        profilesWritten = 0;
        error = null;
        var solDir = BreakpointsFileService.GetWorkspaceRoot(solutionPath);
        if (string.IsNullOrEmpty(solDir))
        {
            error = "workspace_root_unresolved";
            return false;
        }

        var projDir = Path.GetDirectoryName(projectPathRelativeToSolution.Trim().Replace('/', Path.DirectorySeparatorChar));
        var jsonPath = Path.Combine(solDir, projDir ?? string.Empty, "Properties", "launchSettings.json");
        if (!File.Exists(jsonPath))
        {
            error = "launch_settings_not_found: " + jsonPath;
            return false;
        }

        string json;
        try { json = File.ReadAllText(jsonPath); }
        catch (Exception ex)
        {
            error = "launch_settings_read: " + ex.Message;
            return false;
        }

        if (!LaunchSettingsJsonImport.TryReadProjectProfiles(json, projectPathRelativeToSolution, out var list, out var perr))
        {
            error = perr;
            return false;
        }

        if (!TryLoadDocument(solutionPath, out var doc, out _) || doc is null)
            doc = new LaunchProfilesTomlModel { Version = 1, ActiveProfile = DefaultProfileId, Profiles = [] };
        doc.Profiles ??= new Dictionary<string, LaunchProfileModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, model) in list)
        {
            doc.Profiles[name] = model;
            profilesWritten++;
        }

        if (string.IsNullOrWhiteSpace(doc.ActiveProfile) || !doc.Profiles.ContainsKey(doc.ActiveProfile!))
            doc.ActiveProfile = list[0].Name;

        WriteDocument(solutionPath, doc);
        return true;
    }

    public static void UpsertActiveProject(string solutionPath, string projectPathRelativeToSolution)
    {
        if (!TryLoadDocument(solutionPath, out var doc, out _) || doc is null)
            doc = new LaunchProfilesTomlModel { Version = 1, ActiveProfile = DefaultProfileId, Profiles = [] };

        doc.Profiles ??= new Dictionary<string, LaunchProfileModel>(StringComparer.OrdinalIgnoreCase);
        var active = string.IsNullOrWhiteSpace(doc.ActiveProfile) ? DefaultProfileId : doc.ActiveProfile!.Trim();
        doc.ActiveProfile = active;
        if (!doc.Profiles.TryGetValue(active, out var p) || p is null)
        {
            doc.Profiles[active] = new LaunchProfileModel
            {
                Project = null,
                Configuration = DefaultConfiguration
            };
            p = doc.Profiles[active];
        }

        var rel = projectPathRelativeToSolution.Trim().Replace('/', Path.DirectorySeparatorChar);
        p!.Project = rel;

        WriteDocument(solutionPath, doc);
    }

    internal static void WriteDocument(string solutionPath, LaunchProfilesTomlModel doc)
    {
        var path = GetStorePath(solutionPath);
        if (string.IsNullOrEmpty(path))
            return;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var text = CascadeTomlSerializer.Serialize(doc);
        File.WriteAllText(path, text);
    }

    internal static bool TryLoadDocument(
        string solutionPath,
        [NotNullWhen(true)] out LaunchProfilesTomlModel? doc,
        [NotNullWhen(false)] out string? error)
    {
        error = null;
        doc = null;
        var path = GetStorePath(solutionPath);
        if (string.IsNullOrEmpty(path))
        {
            error = "launch_profiles_unavailable";
            return false;
        }

        if (File.Exists(path))
        {
            try
            {
                var text = File.ReadAllText(path);
                var model = CascadeTomlSerializer.Deserialize<LaunchProfilesTomlModel>(text);
                if (model is null)
                {
                    error = "launch_profiles_parse_failed";
                    return false;
                }

                if (model.Version < 1)
                    model.Version = 1;
                doc = model;
                return true;
            }
            catch (Exception ex)
            {
                error = "launch_profiles_parse_failed: " + ex.Message;
                return false;
            }
        }

        if (MigrateFromLegacyJson(solutionPath, out var migrated))
        {
            doc = migrated;
            return true;
        }

        error = "launch_profiles_not_found";
        return false;
    }

    private static bool MigrateFromLegacyJson(string solutionPath, [NotNullWhen(true)] out LaunchProfilesTomlModel? doc)
    {
        doc = null;
        if (!StartupProjectStore.TryLoadLegacyJsonOnly(solutionPath, out var rel) || string.IsNullOrEmpty(rel))
            return false;

        doc = new LaunchProfilesTomlModel
        {
            Version = 1,
            ActiveProfile = DefaultProfileId,
            Profiles = new Dictionary<string, LaunchProfileModel>(StringComparer.OrdinalIgnoreCase)
            {
                [DefaultProfileId] = new LaunchProfileModel
                {
                    Project = rel,
                    Configuration = DefaultConfiguration
                }
            }
        };
        WriteDocument(solutionPath, doc);
        return true;
    }
}

/// <summary>Данные одного launch profile для MSBuild + DAP.</summary>
public readonly record struct LaunchProfileData(
    string ProfileId,
    string ProjectRelativeToSolution,
    string Configuration,
    IReadOnlyList<string>? ProgramArgs,
    string? WorkingDirectoryRelative,
    IReadOnlyDictionary<string, string> Environment,
    bool OpenLaunchBrowser,
    // launchUrl / launch_url: full URL or path; merged with first application URL if relative.
    string? LaunchUrl);

internal sealed class LaunchProfilesTomlModel
{
    public int Version { get; set; } = 1;
    public string? ActiveProfile { get; set; }
    public Dictionary<string, LaunchProfileModel>? Profiles { get; set; }
}

internal sealed class LaunchProfileModel
{
    public string? Project { get; set; }
    public string? Configuration { get; set; }
    public List<string>? ProgramArgs { get; set; }
    public string? WorkingDirectory { get; set; }
    public string? ApplicationUrls { get; set; }
    public string? LaunchUrl { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public bool? LaunchBrowser { get; set; }
}
