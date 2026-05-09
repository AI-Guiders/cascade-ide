#nullable enable

using CascadeIDE.Contracts;

namespace CascadeIDE.Features.EnvironmentReadiness.DataAcquisition;

/// <summary>Классификация путей env для agent-notes (DAL, без UI-текстов).</summary>
public enum AgentNotesFilePathKind
{
    Unset,
    ParentDirForGlobalFile,
    FileExists,
    ParentMissing,
    InvalidPath
}

public enum AgentNotesCanonPathKind
{
    Unset,
    DirectoryExists,
    DirectoryMissing,
    InvalidPath
}

public enum NetcoreDbgPathKind
{
    UnsetFoundOnPath,
    UnsetNotOnPath,
    ExplicitResolved,
    InvalidPath,
    ExplicitBareNameNotInPath,
    ExplicitFilePathMissing
}

/// <summary>Fs/PATH-логика для строк env-готовности; Application только маппит в AnnunciatorLampItem.</summary>
[IoBoundary]
public static class EnvironmentReadinessPathAcquisition
{
    public static AgentNotesFilePathKind ClassifyAgentNotesFilePath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return AgentNotesFilePathKind.Unset;

        try
        {
            var full = CanonicalFilePath.Normalize(raw.Trim());
            var parent = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                return AgentNotesFilePathKind.ParentDirForGlobalFile;

            if (File.Exists(full))
                return AgentNotesFilePathKind.FileExists;

            return AgentNotesFilePathKind.ParentMissing;
        }
        catch
        {
            return AgentNotesFilePathKind.InvalidPath;
        }
    }

    public static AgentNotesCanonPathKind ClassifyAgentNotesCanonPath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return AgentNotesCanonPathKind.Unset;

        try
        {
            var full = CanonicalFilePath.Normalize(raw.Trim());
            if (Directory.Exists(full))
                return AgentNotesCanonPathKind.DirectoryExists;

            return AgentNotesCanonPathKind.DirectoryMissing;
        }
        catch
        {
            return AgentNotesCanonPathKind.InvalidPath;
        }
    }

    public static NetcoreDbgPathKind ClassifyNetcoreDbgPath(
        string? raw,
        Func<string?>? tryResolveNetcoreDbgWhenUnset = null)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            var onPath = tryResolveNetcoreDbgWhenUnset is not null
                ? tryResolveNetcoreDbgWhenUnset.Invoke()
                : EnvironmentReadinessExecutablePathProbe.TryResolveExecutablePath("netcoredbg");
            return onPath is not null
                ? NetcoreDbgPathKind.UnsetFoundOnPath
                : NetcoreDbgPathKind.UnsetNotOnPath;
        }

        var trimmed = raw.Trim();
        if (EnvironmentReadinessExecutablePathProbe.TryResolveExecutablePath(trimmed) is not null)
            return NetcoreDbgPathKind.ExplicitResolved;

        try
        {
            _ = CanonicalFilePath.Normalize(trimmed);
        }
        catch
        {
            return NetcoreDbgPathKind.InvalidPath;
        }

        if (EnvironmentReadinessExecutablePathProbe.IsBareExecutableName(trimmed))
            return NetcoreDbgPathKind.ExplicitBareNameNotInPath;

        return NetcoreDbgPathKind.ExplicitFilePathMissing;
    }
}
