namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Разрешает путь к <c>CascadeIDE.BuildVerifyWorker.dll</c> для out-of-proc verify.</summary>
public static class BuildVerifyWorkerAssemblyLocator
{
    public const string DefaultDllName = "CascadeIDE.BuildVerifyWorker.dll";

    public static bool TryResolve(string? configuredPath, out string dllPath)
    {
        if (TryExistingFile(configuredPath, out dllPath))
            return true;

        var nextToApp = Path.Combine(AppContext.BaseDirectory, DefaultDllName);
        if (TryExistingFile(nextToApp, out dllPath))
            return true;

        var repoTools = TryFindRepoToolsDll();
        if (repoTools is not null && TryExistingFile(repoTools, out dllPath))
            return true;

        dllPath = nextToApp;
        return false;
    }

    private static string? TryFindRepoToolsDll()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CascadeIDE.sln")))
            {
                return Path.Combine(
                    dir.FullName,
                    "tools",
                    "CascadeIDE.BuildVerifyWorker",
                    "bin",
                    "Debug",
                    "net10.0",
                    DefaultDllName);
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static bool TryExistingFile(string? path, out string fullPath)
    {
        fullPath = "";
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            fullPath = Path.GetFullPath(path.Trim());
            return File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }
}
