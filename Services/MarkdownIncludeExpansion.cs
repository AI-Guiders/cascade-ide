using System.Text;
using System.Text.RegularExpressions;

namespace CascadeIDE.Services;

/// <summary>
/// Expand non-standard include directives in Markdown/diagram sources for publishing.
/// Canonical syntax: <c>{{ INCLUDE: relative/path/to/file }}</c> (case-insensitive, spaces allowed).
/// </summary>
public static class MarkdownIncludeExpansion
{
    private static readonly Regex IncludeLineRegex = new(
        @"^\s*\{\{\s*include\s*:\s*(?<path>[^}]+?)\s*\}\}\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public sealed record Options(int MaxDepth = 5);

    public static string ExpandMarkdown(string markdown, string markdownFilePath, Options? options = null)
    {
        if (markdown is null)
            return "";
        if (string.IsNullOrWhiteSpace(markdownFilePath))
            throw new ArgumentException("markdownFilePath is required", nameof(markdownFilePath));

        var opts = options ?? new Options();
        var errors = new List<string>();
        var fullMd = CanonicalFilePath.Normalize(markdownFilePath);
        var baseDir = Path.GetDirectoryName(fullMd) ?? Directory.GetCurrentDirectory();
        var stack = new Stack<string>();

        var result = ExpandMarkdownCore(markdown, baseDir, fullMd, opts, stack, depth: 0, errors);
        if (errors.Count > 0)
            throw new IncludeExpansionException(errors);
        return result;
    }

    private static string ExpandMarkdownCore(
        string markdown,
        string baseDir,
        string markdownPathForErrors,
        Options opts,
        Stack<string> stack,
        int depth,
        List<string> errors)
    {
        // Expand includes only inside fenced code blocks to avoid surprising replacements in prose.
        var sb = new StringBuilder(markdown.Length + 128);
        using var reader = new StringReader(markdown);
        string? line;
        bool inFence = false;
        string? fenceMarker = null;

        while ((line = reader.ReadLine()) is not null)
        {
            var trimmed = line.TrimStart();
            if (IsFenceStartOrEnd(trimmed, out var marker))
            {
                if (!inFence)
                {
                    inFence = true;
                    fenceMarker = marker;
                }
                else if (fenceMarker is not null && string.Equals(marker, fenceMarker, StringComparison.Ordinal))
                {
                    inFence = false;
                    fenceMarker = null;
                }

                sb.AppendLine(line);
                continue;
            }

            if (inFence)
            {
                var m = IncludeLineRegex.Match(line);
                if (m.Success)
                {
                    var rel = (m.Groups["path"].Value ?? "").Trim();
                    var expanded = TryExpandIncludeFile(rel, baseDir, markdownPathForErrors, opts, stack, depth, errors);
                    if (expanded is not null)
                    {
                        sb.Append(expanded);
                        if (!expanded.EndsWith('\n'))
                            sb.AppendLine();
                        continue;
                    }
                }
            }

            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private static bool IsFenceStartOrEnd(string trimmedLine, out string marker)
    {
        if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
        {
            marker = "```";
            return true;
        }
        if (trimmedLine.StartsWith("~~~", StringComparison.Ordinal))
        {
            marker = "~~~";
            return true;
        }
        marker = "";
        return false;
    }

    private static string? TryExpandIncludeFile(
        string relativePath,
        string baseDir,
        string markdownPathForErrors,
        Options opts,
        Stack<string> stack,
        int depth,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            errors.Add($"INCLUDE: empty path in '{markdownPathForErrors}'.");
            return null;
        }

        if (depth >= opts.MaxDepth)
        {
            errors.Add($"INCLUDE: max depth {opts.MaxDepth} exceeded while expanding '{relativePath}' in '{markdownPathForErrors}'.");
            return null;
        }

        var full = CanonicalFilePath.Normalize(Path.Combine(baseDir, relativePath));
        if (stack.Contains(full, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add($"INCLUDE: cycle detected: '{full}'.");
            return null;
        }

        if (!File.Exists(full))
        {
            errors.Add($"INCLUDE: file not found: '{full}' (from '{markdownPathForErrors}').");
            return null;
        }

        try
        {
            stack.Push(full);
            var text = File.ReadAllText(full);

            // Included files are typically diagram sources; allow nested includes line-by-line without Markdown fences.
            var expanded = ExpandDiagramTextCore(
                text,
                Path.GetDirectoryName(full) ?? baseDir,
                full,
                opts,
                stack,
                depth + 1,
                errors);

            return expanded;
        }
        catch (Exception ex)
        {
            errors.Add($"INCLUDE: failed to read '{full}': {ex.Message}");
            return null;
        }
        finally
        {
            if (stack.Count > 0 && string.Equals(stack.Peek(), full, StringComparison.OrdinalIgnoreCase))
                stack.Pop();
        }
    }

    private static string ExpandDiagramTextCore(
        string text,
        string baseDir,
        string sourcePathForErrors,
        Options opts,
        Stack<string> stack,
        int depth,
        List<string> errors)
    {
        var sb = new StringBuilder(text.Length + 64);
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var m = IncludeLineRegex.Match(line);
            if (m.Success)
            {
                var rel = (m.Groups["path"].Value ?? "").Trim();
                var expanded = TryExpandIncludeFile(rel, baseDir, sourcePathForErrors, opts, stack, depth, errors);
                if (expanded is not null)
                {
                    sb.Append(expanded);
                    if (!expanded.EndsWith('\n'))
                        sb.AppendLine();
                    continue;
                }
            }

            sb.AppendLine(line);
        }
        return sb.ToString();
    }
}

public sealed class IncludeExpansionException : Exception
{
    public IncludeExpansionException(IReadOnlyList<string> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors ?? Array.Empty<string>();
    }

    public IReadOnlyList<string> Errors { get; }

    private static string BuildMessage(IReadOnlyList<string> errors)
    {
        if (errors is null || errors.Count == 0)
            return "Include expansion failed.";
        return "Include expansion failed:\n- " + string.Join("\n- ", errors);
    }
}

