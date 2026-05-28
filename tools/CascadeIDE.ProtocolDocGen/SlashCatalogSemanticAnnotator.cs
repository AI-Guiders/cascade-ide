using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CascadeIDE.Services;
using Tomlyn;
using Tomlyn.Model;

/// <summary>Добавляет domain/object/intent/path_role в <c>intent-catalog.toml</c> (ADR 0154).</summary>
internal static class SlashCatalogSemanticAnnotator
{
    private static readonly TomlSerializerOptions TomlOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static readonly Regex PathLine =
        new(@"^\s*path\s*=\s*""(?<path>[^""]+)""\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DomainLine =
        new(@"^\s*domain\s*=", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static int AnnotateFile(string catalogPath)
    {
        var text = File.ReadAllText(catalogPath, Encoding.UTF8);
        var lines = text.Replace("\r\n", "\n").Split('\n');
        var mapLevels = loadMapLevels(catalogPath);
        var changed = 0;
        var i = 0;

        while (i < lines.Length)
        {
            if (!lines[i].Trim().Equals("[[command.form.slash]]", StringComparison.Ordinal))
            {
                i++;
                continue;
            }

            var blockEnd = findBlockEnd(lines, i + 1);
            if (!tryFindPath(lines, i + 1, blockEnd, out var pathLine, out var path))
            {
                i = blockEnd;
                continue;
            }

            if (blockHasDomain(lines, i + 1, blockEnd))
            {
                i = blockEnd;
                continue;
            }

            mapLevels.TryGetValue(path, out var mapLevel);
            var fields = SlashRouteSemantics.Resolve(path, mapLevel);
            var insert = buildInsertLines(fields);
            lines = insertLines(lines, pathLine + 1, insert);
            blockEnd += insert.Length;
            changed++;
            i = blockEnd;
        }

        if (changed > 0)
        {
            var outText = string.Join(Environment.NewLine, lines);
            if (text.EndsWith('\n') && !outText.EndsWith(Environment.NewLine, StringComparison.Ordinal))
                outText += Environment.NewLine;
            File.WriteAllText(catalogPath, outText, Encoding.UTF8);
        }

        Console.WriteLine($"Annotated {changed} slash block(s) in {catalogPath}");
        return changed;
    }

    private static Dictionary<string, string?> loadMapLevels(string catalogPath)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var text = File.ReadAllText(catalogPath, Encoding.UTF8);
        if (TomlSerializer.Deserialize<TomlTable>(text, TomlOptions) is not TomlTable root)
            return result;

        if (!root.TryGetValue("command", out var commandNode))
            return result;

        collectMapLevels(commandNode, result);
        return result;
    }

    private static void collectMapLevels(object? commandNode, Dictionary<string, string?> mapLevels)
    {
        switch (commandNode)
        {
            case TomlTableArray commands:
                foreach (var command in commands)
                    collectSlashRows(command, mapLevels);
                break;
            case TomlTable single:
                collectSlashRows(single, mapLevels);
                break;
        }
    }

    private static void collectSlashRows(TomlTable command, Dictionary<string, string?> mapLevels)
    {
        if (command.TryGetValue("slash", out var slashNode))
            walkSlashRows(slashNode, mapLevels);

        if (command.TryGetValue("form", out var formNode) && formNode is TomlTable form
            && form.TryGetValue("slash", out var formSlash))
            walkSlashRows(formSlash, mapLevels);
    }

    private static void walkSlashRows(object? slashNode, Dictionary<string, string?> mapLevels)
    {
        switch (slashNode)
        {
            case TomlTableArray rows:
                foreach (var row in rows)
                    addMapLevel(row, mapLevels);
                break;
            case TomlTable single:
                addMapLevel(single, mapLevels);
                break;
        }
    }

    private static void addMapLevel(TomlTable row, Dictionary<string, string?> mapLevels)
    {
        if (!row.TryGetValue("path", out var pathNode))
            return;

        var path = IntentCatalogSlashPathCollector.normalizeSlashPath(pathNode.ToString());
        if (path.Length == 0)
            return;

        string? level = null;
        if (row.TryGetValue("args", out var argsNode) && argsNode is TomlTable args
            && args.TryGetValue("level", out var levelNode))
        {
            level = levelNode.ToString()?.Trim();
        }

        mapLevels[path] = level;
    }

    private static int findBlockEnd(string[] lines, int start)
    {
        for (var i = start; i < lines.Length; i++)
        {
            var t = lines[i].Trim();
            if (t.StartsWith("[[", StringComparison.Ordinal) && !t.Equals("[[command.form.slash]]", StringComparison.Ordinal))
                return i;
            if (t.StartsWith("# ===", StringComparison.Ordinal))
                return i;
        }

        return lines.Length;
    }

    private static bool tryFindPath(string[] lines, int start, int end, out int pathLine, out string path)
    {
        pathLine = -1;
        path = "";
        for (var i = start; i < end; i++)
        {
            var m = PathLine.Match(lines[i]);
            if (!m.Success)
                continue;

            pathLine = i;
            path = IntentCatalogSlashPathCollector.normalizeSlashPath(m.Groups["path"].Value);
            return path.Length > 0;
        }

        return false;
    }

    private static bool blockHasDomain(string[] lines, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            if (DomainLine.IsMatch(lines[i]))
                return true;
        }

        return false;
    }

    private static string[] buildInsertLines(SlashSemanticFields fields)
    {
        var role = fields.PathRole == SlashPathRole.Alias ? "alias" : "canonical";
        var list = new List<string>
        {
            $"domain = \"{fields.Domain}\"",
        };

        if (!string.IsNullOrEmpty(fields.Object))
            list.Add($"object = \"{fields.Object}\"");

        if (!string.IsNullOrEmpty(fields.Intent))
            list.Add($"intent = \"{fields.Intent}\"");

        if (fields.PathRole == SlashPathRole.Alias)
            list.Add($"path_role = \"{role}\"");

        return list.ToArray();
    }

    private static string[] insertLines(string[] lines, int index, string[] insert)
    {
        var result = new string[lines.Length + insert.Length];
        Array.Copy(lines, 0, result, 0, index);
        Array.Copy(insert, 0, result, index, insert.Length);
        Array.Copy(lines, index, result, index + insert.Length, lines.Length - index);
        return result;
    }
}
