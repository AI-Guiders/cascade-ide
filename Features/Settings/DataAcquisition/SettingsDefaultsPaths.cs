#nullable enable
using System.Reflection;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Settings.DataAcquisition;

/// <summary>DAL: discovery + read for factory <c>Settings/defaults-settings.toml</c> (disk / walk-up / embedded).</summary>
[IoBoundary]
public static class SettingsDefaultsPaths
{
    public const string BundledRelativePath = "Settings/defaults-settings.toml";
    public const string EmbeddedMarker = "embedded:Settings/defaults-settings.toml";

    public static string GetUnderBaseDirectory(string? baseDirectory = null) =>
        Path.Combine(
            string.IsNullOrWhiteSpace(baseDirectory) ? AppContext.BaseDirectory : baseDirectory,
            "Settings",
            "defaults-settings.toml");

    /// <summary>explicit → BaseDirectory → walk-up from cwd (or <paramref name="startDirectory"/>).</summary>
    public static string? TryFindOnDisk(
        string? explicitPath = null,
        string? startDirectory = null,
        int maxWalkLevels = 8)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
            return explicitPath;

        var underBase = GetUnderBaseDirectory();
        if (File.Exists(underBase))
            return underBase;

        return WalkUp(startDirectory, maxWalkLevels);
    }

    public static string? TryReadEmbedded(Assembly assembly)
    {
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.EndsWith("defaults-settings.toml", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("defaults-settings.toml", StringComparison.OrdinalIgnoreCase))
                continue;

            using var stream = assembly.GetManifestResourceStream(name);
            if (stream is null)
                continue;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        return null;
    }

    /// <summary>Disk first, then optional embedded assembly. <paramref name="resolvedPath"/> is disk path or <see cref="EmbeddedMarker"/>.</summary>
    public static string? TryReadToml(
        string? explicitPath,
        Assembly? embeddedAssembly,
        out string? resolvedPath)
    {
        resolvedPath = null;
        var disk = TryFindOnDisk(explicitPath);
        if (disk is not null)
        {
            var text = TextFileReadWrite.TryReadAllTextIfExists(disk);
            if (text is not null)
            {
                resolvedPath = disk;
                return text;
            }
        }

        if (embeddedAssembly is null)
            return null;

        var embedded = TryReadEmbedded(embeddedAssembly);
        if (embedded is null)
            return null;

        resolvedPath = EmbeddedMarker;
        return embedded;
    }

    static string? WalkUp(string? startDirectory, int maxWalkLevels)
    {
        try
        {
            var dir = new DirectoryInfo(
                string.IsNullOrWhiteSpace(startDirectory)
                    ? Environment.CurrentDirectory
                    : startDirectory);
            for (var i = 0; i < maxWalkLevels && dir is not null; i++, dir = dir.Parent)
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
}
