using System.Collections.Concurrent;
using DotNet.Globbing;

namespace CascadeIDE.Services;

/// <summary>
/// Правила из <c>.gitignore</c> и <c>.cascadeignore</c> в корне репозитория (аналог подавления лишнего в дереве, как .gitignore / VS).
/// Сопоставление путей — упрощённое подмножество gitignore (без полной семантики negation-слоёв).
/// </summary>
public sealed class WorkspaceIgnoreMatcher
{
    private static readonly ConcurrentDictionary<string, WorkspaceIgnoreMatcher> Cache = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _repositoryRoot;
    private readonly Glob[] _globs;

    private WorkspaceIgnoreMatcher(string repositoryRoot, IReadOnlyList<Glob> globs)
    {
        _repositoryRoot = repositoryRoot;
        _globs = globs.Count > 0 ? globs.ToArray() : Array.Empty<Glob>();
    }

    /// <summary>Корень репозитория Git или <paramref name="fallbackStartDirectory"/>.</summary>
    public static string ResolveRepositoryRoot(string fallbackStartDirectory)
    {
        fallbackStartDirectory = fallbackStartDirectory.Trim();
        if (fallbackStartDirectory.Length == 0)
            return fallbackStartDirectory;

        try
        {
            var dir = new DirectoryInfo(Path.GetFullPath(fallbackStartDirectory));
            while (dir is not null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                    return dir.FullName;
                dir = dir.Parent;
            }
        }
        catch
        {
            return Path.GetFullPath(fallbackStartDirectory);
        }

        try
        {
            return Path.GetFullPath(fallbackStartDirectory);
        }
        catch
        {
            return fallbackStartDirectory;
        }
    }

    /// <summary>Кэш по нормализованному корню репозитория.</summary>
    public static WorkspaceIgnoreMatcher GetOrCreate(string repositoryRoot)
    {
        var key = Path.GetFullPath(repositoryRoot.Trim());
        return Cache.GetOrAdd(key, static k => Load(k));
    }

    private static WorkspaceIgnoreMatcher Load(string repositoryRoot)
    {
        var globs = new List<Glob>();
        var options = new GlobOptions { Evaluation = new EvaluationOptions { CaseInsensitive = true } };

        void AppendFile(string path)
        {
            if (!File.Exists(path))
                return;
            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch
            {
                return;
            }

            foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
            {
                foreach (var pattern in GitIgnoreLineToGlobPatterns(line))
                {
                    try
                    {
                        globs.Add(Glob.Parse(pattern, options));
                    }
                    catch
                    {
                        // пропускаем неподдерживаемые шаблоны
                    }
                }
            }
        }

        AppendFile(Path.Combine(repositoryRoot, ".gitignore"));
        AppendFile(Path.Combine(repositoryRoot, ".cascadeignore"));

        return new WorkspaceIgnoreMatcher(repositoryRoot, globs);
    }

    /// <summary>Очистка кэша (тесты).</summary>
    internal static void ClearCacheForTests() => Cache.Clear();

    /// <param name="fullPath">Абсолютный путь к файлу или каталогу.</param>
    public bool IsIgnored(string fullPath)
    {
        if (_globs.Length == 0)
            return false;

        string rel;
        try
        {
            rel = Path.GetRelativePath(_repositoryRoot, fullPath);
        }
        catch
        {
            return false;
        }

        rel = rel.Replace('\\', '/');
        if (rel.StartsWith("..", StringComparison.Ordinal))
            return false;

        foreach (var g in _globs)
        {
            if (g.IsMatch(rel))
                return true;
        }

        return false;
    }

    /// <summary>Преобразование строки gitignore в один или несколько glob-паттернов DotNet.Glob.</summary>
    internal static IEnumerable<string> GitIgnoreLineToGlobPatterns(string rawLine)
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line[0] == '#')
            yield break;

        // Negation — в v1 не поддерживаем (нужен порядок правил как в git)
        if (line[0] == '!')
            yield break;

        var anchored = line[0] == '/';
        if (anchored)
            line = line[1..];

        var dirOnly = line.EndsWith('/');
        if (dirOnly && line.Length > 0)
            line = line[..^1];

        if (line.Length == 0)
            yield break;

        line = line.Replace('\\', '/');

        if (line.Contains('/'))
        {
            if (dirOnly)
            {
                if (anchored)
                {
                    yield return line + "/**";
                    yield return line;
                }
                else
                {
                    yield return "**/" + line + "/**";
                    yield return "**/" + line;
                }
            }
            else
            {
                if (anchored)
                {
                    yield return line;
                }
                else
                {
                    yield return "**/" + line;
                }
            }

            yield break;
        }

        // Нет слэша — шаблон применяется в любом каталоге
        if (dirOnly)
        {
            yield return "**/" + line + "/**";
            yield return "**/" + line;
        }
        else
        {
            yield return "**/" + line;
            if (line.Contains('*'))
                yield return line;
        }
    }
}
