#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CascadeIDE.Services;

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
        if (el.ValueKind != JsonValueKind.Object)
            return false;

        if (!el.TryGetProperty("commandName", out var cn) || cn.ValueKind != JsonValueKind.String)
            return false;

        if (!string.Equals(cn.GetString(), "Project", StringComparison.Ordinal))
            return false;

        var m = new LaunchProfileModel
        {
            Project = projectRelative,
            Configuration = LaunchProfilesStore.DefaultConfiguration
        };

        if (el.TryGetProperty("commandLineArgs", out var cla) && cla.ValueKind == JsonValueKind.String)
        {
            var s = cla.GetString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                var t = s.Trim();
                if (t.Length > 0)
                    m.ProgramArgs = t.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }

        if (el.TryGetProperty("applicationUrl", out var au) && au.ValueKind == JsonValueKind.String)
        {
            var u = au.GetString();
            if (!string.IsNullOrWhiteSpace(u))
                m.ApplicationUrls = u!.Trim();
        }

        if (el.TryGetProperty("launchUrl", out var lurl) && lurl.ValueKind == JsonValueKind.String)
        {
            var s = lurl.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                m.LaunchUrl = s!.Trim();
        }

        if (el.TryGetProperty("launchBrowser", out var lb) && (lb.ValueKind == JsonValueKind.True || lb.ValueKind == JsonValueKind.False))
            m.LaunchBrowser = lb.GetBoolean();
        m.Env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (el.TryGetProperty("environmentVariables", out var ev) && ev.ValueKind == JsonValueKind.Object)
        {
            foreach (var v in ev.EnumerateObject())
            {
                var val = v.Value.ValueKind == JsonValueKind.String
                    ? (v.Value.GetString() ?? "")
                    : v.Value.ToString();
                if (!string.IsNullOrEmpty(v.Name))
                    m.Env[v.Name] = val;
            }
        }

        if (m.LaunchBrowser is not true &&
            m.ApplicationUrls is { Length: > 0 } && m.ApplicationUrls.Contains("htt", StringComparison.OrdinalIgnoreCase))
        {
            m.LaunchBrowser = true;
        }

        model = m;
        return true;
    }
}
