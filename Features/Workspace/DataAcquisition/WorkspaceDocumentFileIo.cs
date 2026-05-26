#nullable enable
using System.Text;
using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace.DataAcquisition;

/// <summary>Чтение/запись текстовых файлов workspace для агента (DAL, ADR 0102).</summary>
[IoBoundary]
public static class WorkspaceDocumentFileIo
{
    public static bool TryResolvePath(
        string? workspaceRoot,
        IReadOnlyList<string>? solutionRoots,
        string filePath,
        out string fullPath,
        out string? error)
    {
        fullPath = "";
        error = null;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            error = "file_path is required.";
            return false;
        }

        var candidate = filePath.Trim();
        if (!Path.IsPathRooted(candidate))
        {
            if (string.IsNullOrWhiteSpace(workspaceRoot))
            {
                error = "workspace is not open.";
                return false;
            }

            candidate = Path.GetFullPath(Path.Combine(workspaceRoot.Trim(), candidate));
        }
        else if (!CanonicalFilePath.TryNormalize(candidate, out candidate))
        {
            error = "invalid file_path.";
            return false;
        }

        if (!IsUnderAllowedRoots(candidate, workspaceRoot, solutionRoots))
        {
            error = "path is outside workspace.";
            return false;
        }

        fullPath = candidate;
        return true;
    }

    public static bool TryReadText(
        string fullPath,
        int? offsetLine,
        int? limitLines,
        int? maxChars,
        out string json,
        out string? error)
    {
        json = "";
        error = null;
        if (!File.Exists(fullPath))
        {
            error = "file not found.";
            return false;
        }

        try
        {
            var text = ReadAllTextShared(fullPath);
            var totalLength = text.Length;
            if (offsetLine is > 0 || limitLines is > 0)
                text = SliceByLines(text, offsetLine, limitLines);

            var truncated = false;
            if (maxChars is > 0 && text.Length > maxChars.Value)
            {
                text = text[..maxChars.Value];
                truncated = true;
            }

            json = System.Text.Json.JsonSerializer.Serialize(new
            {
                file_path = fullPath,
                length = totalLength,
                returned_length = text.Length,
                truncated,
                offset = offsetLine,
                limit = limitLines,
                text
            });
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryWriteText(string fullPath, string content, bool createIfMissing, out string? error)
    {
        error = null;
        try
        {
            if (!File.Exists(fullPath))
            {
                if (!createIfMissing)
                {
                    error = "file not found.";
                    return false;
                }

                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
            }

            File.WriteAllText(fullPath, content ?? "", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool IsUnderAllowedRoots(
        string fullPath,
        string? workspaceRoot,
        IReadOnlyList<string>? solutionRoots)
    {
        if (!string.IsNullOrWhiteSpace(workspaceRoot)
            && CanonicalFilePath.TryNormalize(workspaceRoot.Trim(), out var ws)
            && IsPathUnderRoot(fullPath, ws))
            return true;

        if (solutionRoots is null)
            return false;

        foreach (var root in solutionRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;
            if (!CanonicalFilePath.TryNormalize(root.Trim(), out var normalized))
                continue;
            if (IsPathUnderRoot(fullPath, normalized))
                return true;
        }

        return false;
    }

    private static bool IsPathUnderRoot(string fullPath, string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory) && !File.Exists(rootDirectory))
            return false;

        var root = Path.GetFullPath(rootDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(fullPath);
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadAllTextShared(string path)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return reader.ReadToEnd();
            }
            catch (IOException) when (attempt < 2)
            {
                Thread.Sleep(40);
            }
            catch (UnauthorizedAccessException) when (attempt < 2)
            {
                Thread.Sleep(40);
            }
        }

        return File.ReadAllText(path);
    }

    private static string SliceByLines(string text, int? offsetLine, int? limitLines)
    {
        var lines = text.Split('\n');
        var start = offsetLine is > 0 ? Math.Clamp(offsetLine.Value, 1, lines.Length) : 1;
        var end = limitLines is > 0
            ? Math.Min(lines.Length, start + limitLines.Value - 1)
            : lines.Length;
        if (start > lines.Length)
            return "";
        return string.Join('\n', lines.Skip(start - 1).Take(end - start + 1));
    }
}
