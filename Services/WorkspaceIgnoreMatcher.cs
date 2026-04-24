using System.Collections.Concurrent;
using DotNet.Globbing;

namespace CascadeIDE.Services;

/// <summary>
/// Правила из вложенных <c>.gitignore</c> и <c>.cascadeignore</c> (в каждом каталоге свой файл).
/// Семантика близка к git: последнее совпадение побеждает; <c>!</c> снимает игнор.
/// </summary>
public sealed class WorkspaceIgnoreMatcher
{
    private static readonly ConcurrentDictionary<string, WorkspaceIgnoreMatcher> Cache = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _repositoryRoot;
    private readonly CompiledRule[] _rules;

    private WorkspaceIgnoreMatcher(string repositoryRoot, IReadOnlyList<CompiledRule> rules)
    {
        _repositoryRoot = repositoryRoot;
        _rules = rules.Count > 0 ? rules.ToArray() : Array.Empty<CompiledRule>();
    }

    private sealed class CompiledRule
    {
        public required string BaseDir { get; init; }
        public required Glob[] Globs { get; init; }
        /// <summary>Строка была с префиксом <c>!</c> — совпадение отменяет игнор.</summary>
        public required bool Negation { get; init; }

        public bool Matches(string relativePathFromBaseDir)
        {
            relativePathFromBaseDir = relativePathFromBaseDir.Replace('\\', '/');
            if (relativePathFromBaseDir.StartsWith("..", StringComparison.Ordinal))
                return false;
            foreach (var g in Globs)
            {
                if (g.IsMatch(relativePathFromBaseDir))
                    return true;
            }

            return false;
        }
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
        var options = new GlobOptions { Evaluation = new EvaluationOptions { CaseInsensitive = true } };
        var rules = new List<CompiledRule>();

        foreach (var ignoreFile in EnumerateIgnoreFilesSorted(repositoryRoot))
            AppendRulesFromIgnoreFile(ignoreFile, options, rules);

        return new WorkspaceIgnoreMatcher(repositoryRoot, rules);
    }

    private static void AppendRulesFromIgnoreFile(string ignoreFile, GlobOptions options, List<CompiledRule> rules)
    {
        string text;
        try
        {
            text = File.ReadAllText(ignoreFile);
        }
        catch
        {
            return;
        }

        var baseDir = Path.GetDirectoryName(ignoreFile);
        if (string.IsNullOrEmpty(baseDir))
            return;
        try
        {
            baseDir = Path.GetFullPath(baseDir);
        }
        catch
        {
            return;
        }

        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (!TryParseGitIgnoreLine(line, out var negation, out var patternBodies))
                continue;

            foreach (var pl in patternBodies)
            {
                var globs = new List<Glob>();
                foreach (var pattern in GitIgnoreLineToGlobPatterns(pl))
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

                if (globs.Count > 0)
                {
                    rules.Add(new CompiledRule
                    {
                        BaseDir = baseDir,
                        Globs = globs.ToArray(),
                        Negation = negation,
                    });
                }
            }
        }
    }

    /// <summary>Все <c>.gitignore</c> и <c>.cascadeignore</c> под корнем, без захода в <c>.git</c>; в каталоге сначала gitignore, потом cascade.</summary>
    internal static IReadOnlyList<string> EnumerateIgnoreFilesSorted(string repositoryRoot)
    {
        var list = new List<string>();
        string root;
        try
        {
            root = Path.GetFullPath(repositoryRoot.Trim());
        }
        catch
        {
            return list;
        }

        if (!Directory.Exists(root))
            return list;

        CollectIgnoreFiles(root, list);
        list.Sort(IgnoreFileComparer);

        return list;
    }

    private static void CollectIgnoreFiles(string directory, List<string> list)
    {
        foreach (var name in new[] { ".gitignore", ".cascadeignore" })
        {
            var p = Path.Combine(directory, name);
            if (File.Exists(p))
                list.Add(Path.GetFullPath(p));
        }

        string[] subs;
        try
        {
            subs = Directory.GetDirectories(directory);
        }
        catch
        {
            return;
        }

        foreach (var sub in subs)
        {
            if (string.Equals(Path.GetFileName(sub), ".git", StringComparison.OrdinalIgnoreCase))
                continue;
            CollectIgnoreFiles(sub, list);
        }
    }

    private static int IgnoreFileComparer(string a, string b)
    {
        var da = Path.GetDirectoryName(a) ?? "";
        var db = Path.GetDirectoryName(b) ?? "";
        var c = string.Compare(da, db, StringComparison.OrdinalIgnoreCase);
        if (c != 0)
            return c;

        var fa = Path.GetFileName(a);
        var fb = Path.GetFileName(b);
        if (string.Equals(fa, ".gitignore", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(fb, ".cascadeignore", StringComparison.OrdinalIgnoreCase))
            return -1;
        if (string.Equals(fa, ".cascadeignore", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(fb, ".gitignore", StringComparison.OrdinalIgnoreCase))
            return 1;

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Очистка кэша (тесты).</summary>
    internal static void ClearCacheForTests() => Cache.Clear();

    /// <param name="fullPath">Абсолютный путь к файлу или каталогу.</param>
    public bool IsIgnored(string fullPath)
    {
        if (_rules.Length == 0)
            return false;

        try
        {
            fullPath = Path.GetFullPath(fullPath);
        }
        catch
        {
            return false;
        }

        bool? lastIgnored = null;
        foreach (var rule in _rules)
        {
            string rel;
            try
            {
                rel = Path.GetRelativePath(rule.BaseDir, fullPath);
            }
            catch
            {
                continue;
            }

            rel = rel.Replace('\\', '/');
            if (rel.StartsWith("..", StringComparison.Ordinal))
                continue;

            if (!rule.Matches(rel))
                continue;

            // Совпало: позитивное правило → игнор; negation → не игнор
            lastIgnored = !rule.Negation;
        }

        return lastIgnored == true;
    }

    /// <summary>Разбор одной строки: комментарии и пустые — false.</summary>
    internal static bool TryParseGitIgnoreLine(string rawLine, out bool negation, out IReadOnlyList<string> patternBodies)
    {
        negation = false;
        patternBodies = Array.Empty<string>();
        var line = rawLine.Trim();
        if (line.Length == 0 || line[0] == '#')
            return false;

        if (line[0] == '!')
        {
            negation = true;
            line = line[1..].Trim();
        }

        if (line.Length == 0 || line[0] == '#')
            return false;

        patternBodies = new[] { line };
        return true;
    }

    /// <summary>Преобразование тела строки gitignore (без <c>!</c>) в glob-паттерны DotNet.Glob.</summary>
    internal static IEnumerable<string> GitIgnoreLineToGlobPatterns(string lineAfterNegationStripped)
    {
        var line = lineAfterNegationStripped.Trim();
        if (line.Length == 0 || line[0] == '#')
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
