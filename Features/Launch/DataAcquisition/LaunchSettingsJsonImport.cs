#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CascadeIDE.Features.Launch.DataAcquisition;

/// <summary>
/// Чтение SDK <c>Properties/launchSettings.json</c> и сопоставление с <see cref="LaunchProfileModel"/> (только <c>commandName: Project</c>, Kestrel).
/// </summary>
internal static class LaunchSettingsJsonImport
{
    internal static bool TryReadProjectProfiles(
        string json,
        string projectPathRelativeToSolution,
        [NotNullWhen(true)] out List<(string Name, LaunchProfileModel Model)>? profiles,
        [NotNullWhen(false)] out string? error)
    {
        profiles = null;
        error = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("profiles", out var p) || p.ValueKind != JsonValueKind.Object)
            {
                error = "launch_settings_no_profiles";
                return false;
            }

            var list = new List<(string, LaunchProfileModel)>();
            var proj = projectPathRelativeToSolution.Replace('/', Path.DirectorySeparatorChar);
            foreach (var prop in p.EnumerateObject())
            {
                if (TryMapProjectProfile(prop.Value, proj, out var m))
                    list.Add((prop.Name, m));
            }

            if (list.Count == 0)
            {
                error = "launch_settings_no_project_profiles";
                return false;
            }

            profiles = list;
            return true;
        }
        catch (Exception ex)
        {
            error = "launch_settings_parse: " + ex.Message;
            return false;
        }
    }

    private static bool TryMapProjectProfile(JsonElement el, string projectRelative, [NotNullWhen(true)] out LaunchProfileModel? model)
    {
        model = null;
        if (!IsKestrelProjectProfile(el))
            return false;

        var m = CreateBaseProfileModel(projectRelative);
        ApplyCommandLineArgs(el, m);
        ApplyApplicationUrl(el, m);
        ApplyLaunchUrl(el, m);
        ApplyLaunchBrowserFlag(el, m);
        m.Env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ApplyEnvironmentVariables(el, m);
        ApplyKestrelBrowserHeuristic(m);

        model = m;
        return true;
    }

    private static bool IsKestrelProjectProfile(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object)
            return false;
        if (!el.TryGetProperty("commandName", out var cn) || cn.ValueKind != JsonValueKind.String)
            return false;
        return string.Equals(cn.GetString(), "Project", StringComparison.Ordinal);
    }

    private static LaunchProfileModel CreateBaseProfileModel(string projectRelative) =>
        new()
        {
            Project = projectRelative,
            Configuration = LaunchProfilesStore.DefaultConfiguration
        };

    // Семантика как у SDK: токенизация по пробелу без кавычек; аргументы с пробелами в кавычках не разбираем.
    private static void ApplyCommandLineArgs(JsonElement el, LaunchProfileModel m)
    {
        if (!el.TryGetProperty("commandLineArgs", out var cla) || cla.ValueKind != JsonValueKind.String)
            return;
        var s = cla.GetString();
        if (string.IsNullOrWhiteSpace(s))
            return;
        var t = s.Trim();
        if (t.Length == 0)
            return;
        m.ProgramArgs = t.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static void ApplyApplicationUrl(JsonElement el, LaunchProfileModel m)
    {
        if (!el.TryGetProperty("applicationUrl", out var au) || au.ValueKind != JsonValueKind.String)
            return;
        var u = au.GetString();
        if (!string.IsNullOrWhiteSpace(u))
            m.ApplicationUrls = u.Trim();
    }

    private static void ApplyLaunchUrl(JsonElement el, LaunchProfileModel m)
    {
        if (!el.TryGetProperty("launchUrl", out var lurl) || lurl.ValueKind != JsonValueKind.String)
            return;
        var s = lurl.GetString();
        if (!string.IsNullOrWhiteSpace(s))
            m.LaunchUrl = s.Trim();
    }

    private static void ApplyLaunchBrowserFlag(JsonElement el, LaunchProfileModel m)
    {
        if (!el.TryGetProperty("launchBrowser", out var lb) ||
            (lb.ValueKind != JsonValueKind.True && lb.ValueKind != JsonValueKind.False))
            return;
        m.LaunchBrowser = lb.GetBoolean();
    }

    private static void ApplyEnvironmentVariables(JsonElement el, LaunchProfileModel m)
    {
        if (!el.TryGetProperty("environmentVariables", out var ev) || ev.ValueKind != JsonValueKind.Object)
            return;
        foreach (var v in ev.EnumerateObject())
        {
            if (string.IsNullOrEmpty(v.Name))
                continue;
            var val = v.Value.ValueKind == JsonValueKind.String
                ? (v.Value.GetString() ?? "")
                : v.Value.ToString();
            m.Env![v.Name] = val;
        }
    }

    private static void ApplyKestrelBrowserHeuristic(LaunchProfileModel m)
    {
        if (m.LaunchBrowser is not true && ApplicationUrlsSuggestKestrelListener(m.ApplicationUrls))
            m.LaunchBrowser = true;
    }

    /// <summary>Любой сегмент в <c>applicationUrl</c> (через <c>;</c> как у ASPNETCORE_URLS) с префиксом http(s).</summary>
    internal static bool ApplicationUrlsSuggestKestrelListener(string? applicationUrls)
    {
        if (string.IsNullOrWhiteSpace(applicationUrls))
            return false;
        foreach (var part in applicationUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                part.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
