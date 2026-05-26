#nullable enable
using AgentNotes.Core;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>
/// In-proc init of <see cref="AgentNotesRuntime"/> from <see cref="AgentNotesSettings.ConfigPath"/> (SSOT with MCP <c>--config</c>).
/// </summary>
internal static class AgentNotesRuntimeLoader
{
    private static readonly object Gate = new();
    private static string? s_lastConfigPath;
    private static string? s_loadError;

    public static string? LastLoadError => Volatile.Read(ref s_loadError);

    public static bool EnsureInitialized(CascadeIdeSettings settings)
    {
        var configPath = ResolveConfigPath(settings);
        if (configPath is null)
        {
            lock (Gate)
            {
                AgentNotesRuntime.ClearConfiguration();
                s_lastConfigPath = null;
                s_loadError = null;
            }

            return false;
        }

        lock (Gate)
        {
            if (s_lastConfigPath == configPath && AgentNotesRuntime.IsConfigured)
                return true;

            try
            {
                var local = LocalSettingsLoader.Load(configPath);
                AgentNotesRuntime.Initialize(local, configPath);
                s_lastConfigPath = configPath;
                s_loadError = null;
                return true;
            }
            catch (Exception ex)
            {
                s_loadError = ex.Message;
                AgentNotesRuntime.ClearConfiguration();
                s_lastConfigPath = null;
                return false;
            }
        }
    }

    /// <summary>Absolute path to agent-notes TOML from settings (may be missing on disk).</summary>
    public static string? ResolveConfigPath(CascadeIdeSettings settings)
    {
        var raw = settings.AgentNotes.ResolveConfigPath().Trim();
        if (raw.Length == 0)
            return null;

        return Path.IsPathRooted(raw)
            ? Path.GetFullPath(raw)
            : Path.GetFullPath(Path.Combine(SettingsService.GetSettingsDirectory(), raw));
    }

    /// <summary>Clears loader cache and <see cref="AgentNotesRuntime"/> (tests and config unload).</summary>
    public static void Reset()
    {
        lock (Gate)
        {
            AgentNotesRuntime.ClearConfiguration();
            s_lastConfigPath = null;
            s_loadError = null;
        }
    }
}
