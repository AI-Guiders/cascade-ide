#nullable enable

using System.Text;

namespace CascadeIDE.Services;

/// <summary>
/// Проектные правила для MAF IDE-агента: текст из каталога решения/workspace (не шипнутый промпт приложения).
/// Совместимо с уже принятой конвенцией скрытых файлов под <c>.cascade-ide/</c>.
/// </summary>
internal static class CascadeIdeMafProjectAgentRules
{
    /// <summary>Единый файл правил (проще всего версионировать в git).</summary>
    internal const string SingleFileRelativePath = ".cascade-ide/maf-project-rules.md";

    /// <summary>Необязательная папка: несколько модулей, порядок — по имени файла.</summary>
    internal const string FragmentFolderRelativePath = ".cascade-ide/maf-project-rules";

    internal const int MaxMergedCharacters = 64_000;

    /// <summary>Возвращает объединённый markdown или null, если корень невалиден или нет ни одного источника.</summary>
    internal static string? TryLoadMerged(string? workspaceRootAbsolute)
    {
        var root = workspaceRootAbsolute?.Trim();
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            return null;

        var sb = new StringBuilder(4096);
        var basePath = Path.GetFullPath(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        var singlePath = Path.GetFullPath(Path.Combine(basePath, SingleFileRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        TryAppendUnderRoot(sb, basePath, singlePath, sectionTitle: null);

        var fragmentDir = Path.GetFullPath(Path.Combine(basePath, FragmentFolderRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (Directory.Exists(fragmentDir))
        {
            foreach (var file in Directory.EnumerateFiles(fragmentDir, "*.md", SearchOption.TopDirectoryOnly)
                         .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                var full = Path.GetFullPath(file);
                TryAppendUnderRoot(sb, basePath, full,
                    sectionTitle: "## " + Path.GetFileName(full));
            }
        }

        var text = sb.ToString().Trim();
        if (text.Length == 0)
            return null;

        if (text.Length <= MaxMergedCharacters)
            return text;

        var note =
            $"\n\n<!-- cascade-ide:maf-project-rules truncated; merged length was over {MaxMergedCharacters} chars -->\n";
        return text[..MaxMergedCharacters] + note + $"… (обрезано до {MaxMergedCharacters} символов)";
    }

    /// <summary>Не даём читать файл вне <paramref name="workspaceRootAbsolute"/>.</summary>
    private static void TryAppendUnderRoot(StringBuilder sink, string workspaceRootAbsolute, string candidateAbsolutePath,
        string? sectionTitle)
    {
        if (!File.Exists(candidateAbsolutePath))
            return;

        try
        {
            var canonicalWorkspace = Path.GetFullPath(workspaceRootAbsolute);
            var canonicalFile = Path.GetFullPath(candidateAbsolutePath);
            if (!IsStrictChildOrSameDirectory(canonicalWorkspace, canonicalFile))
                return;

            var body = File.ReadAllText(candidateAbsolutePath);
            if (string.IsNullOrWhiteSpace(body))
                return;

            if (sink.Length > 0)
                sink.Append("\n\n");

            if (!string.IsNullOrEmpty(sectionTitle))
            {
                sink.Append(sectionTitle.Trim());
                sink.Append('\n').Append('\n');
            }

            sink.Append(body.Trim());
        }
        catch (IOException)
        {
            /* ignore unreadable rules */
        }
        catch (UnauthorizedAccessException)
        {
            /* ignore */
        }
    }

    private static bool IsStrictChildOrSameDirectory(string workspaceDir, string filePath)
    {
        workspaceDir = Path.TrimEndingDirectorySeparator(Path.GetFullPath(workspaceDir));
        filePath = Path.GetFullPath(filePath);

        var wr = Path.GetPathRoot(workspaceDir);
        var fr = Path.GetPathRoot(filePath);
        if (string.IsNullOrEmpty(wr) || string.IsNullOrEmpty(fr)
            || !string.Equals(wr, fr, StringComparison.OrdinalIgnoreCase))
            return false;

        var rel = Path.GetRelativePath(workspaceDir, filePath);
        return rel != ".." && !rel.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }
}
