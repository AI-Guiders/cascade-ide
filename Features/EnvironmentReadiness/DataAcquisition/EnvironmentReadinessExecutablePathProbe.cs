#nullable enable

namespace CascadeIDE.Features.EnvironmentReadiness.DataAcquisition;

/// <summary>
/// Проверка существования исполняемого файла: явный путь или поиск по PATH (+ PATHEXT на Windows), как для NETCOREDBG_PATH (ADR 0023, DAL).
/// </summary>
public static class EnvironmentReadinessExecutablePathProbe
{
    /// <summary>Возвращает полный путь к существующему файлу или null.</summary>
    public static string? TryResolveExecutablePath(string raw)
    {
        var t = raw.Trim();
        if (t.Length == 0)
            return null;

        if (File.Exists(t))
            return Path.GetFullPath(t);

        try
        {
            var full = Path.GetFullPath(t);
            if (File.Exists(full))
                return full;
        }
        catch
        {
            return null;
        }

        if (!IsBareExecutableName(t))
            return null;

        return SearchPathForExecutable(t);
    }

    /// <summary>Имя команды без каталога (ищем в PATH).</summary>
    public static bool IsBareExecutableName(string t)
    {
        if (t.Length == 0)
            return false;
        return t.IndexOf(Path.DirectorySeparatorChar) < 0 && t.IndexOf(Path.AltDirectorySeparatorChar) < 0;
    }

    private static string? SearchPathForExecutable(string fileName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
            return null;

        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var d = dir.Trim().Trim('"');
            if (d.Length == 0)
                continue;

            foreach (var name in CandidateFileNames(fileName))
            {
                string p;
                try
                {
                    p = Path.Combine(d, name);
                }
                catch
                {
                    continue;
                }

                if (File.Exists(p))
                {
                    try
                    {
                        return Path.GetFullPath(p);
                    }
                    catch
                    {
                        return p;
                    }
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateFileNames(string fileName)
    {
        if (OperatingSystem.IsWindows() && !Path.HasExtension(fileName))
        {
            var pathext = Environment.GetEnvironmentVariable("PATHEXT");
            if (!string.IsNullOrEmpty(pathext))
            {
                foreach (var ext in pathext.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (ext.Length > 0)
                        yield return fileName + ext;
                }
            }
            else
            {
                yield return fileName + ".EXE";
                yield return fileName + ".CMD";
                yield return fileName + ".BAT";
            }

            yield return fileName;
        }
        else
        {
            yield return fileName;
        }
    }
}
