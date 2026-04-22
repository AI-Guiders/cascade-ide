using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>
/// Параметрические формы Command Melody из ADR 0081.
/// Первый релиз ограничен палитрой <c>c:</c> и диапазоном строк для editor-line операций.
/// </summary>
public static class ParametricIntentMelody
{
    private static readonly HashSet<string> PaletteOnlyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "els",
        "eld",
        "rmx",
        "rix",
    };

    public sealed record ParsedLineRange(string Alias, string DisplayTail, int StartLine, int EndLine);

    public static bool IsPaletteOnlyAlias(string alias) =>
        !string.IsNullOrWhiteSpace(alias) && PaletteOnlyAliases.Contains(alias.Trim());

    public static bool IsChordEligibleAlias(string alias) => !IsPaletteOnlyAlias(alias);

    public static string? TryGetAliasPrefixBeforeColon(string tailNormalized)
    {
        if (string.IsNullOrWhiteSpace(tailNormalized))
            return null;

        var idx = tailNormalized.IndexOf(':');
        if (idx <= 0)
            return null;

        var alias = tailNormalized[..idx].Trim();
        return string.IsNullOrEmpty(alias) ? null : alias;
    }

    public static bool TryParseLineRangeTail(string tailNormalized, out ParsedLineRange? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(tailNormalized))
            return false;

        var parts = tailNormalized.Split(':');
        if (parts.Length != 3)
            return false;

        var alias = parts[0].Trim().ToLowerInvariant();
        if (!IsPaletteOnlyAlias(alias))
            return false;

        if (!int.TryParse(parts[1], out var startLine) || !int.TryParse(parts[2], out var endLine))
            return false;

        parsed = new ParsedLineRange(alias, $"{alias}:{startLine}:{endLine}", startLine, endLine);
        return true;
    }

    public static string BuildAliasUsageHint(string alias) =>
        alias.ToLowerInvariant() switch
        {
            "els" => "c:els:<start>:<end> - выделить строки в активном документе",
            "eld" => "c:eld:<start>:<end> - удалить строки в активном документе",
            "rmx" => "c:rmx:<start>:<end> - Extract Method по диапазону (ещё не подключено)",
            "rix" => "c:rix:<start>:<end> - Extract Interface по диапазону (ещё не подключено)",
            _ => $"c:{alias}:<start>:<end>"
        };

    public static string BuildAliasUsageCategory(string alias) =>
        alias.ToLowerInvariant() switch
        {
            "els" => "Editor -> Line -> Select",
            "eld" => "Editor -> Line -> Delete",
            "rmx" => "Refactor -> Method -> Extract",
            "rix" => "Refactor -> Interface -> Extract",
            _ => "Parametric melody"
        };

    public static bool TryBuildExecutionArgs(
        ParsedLineRange parsed,
        string? currentFilePath,
        string? editorText,
        out string commandId,
        out string argsJson,
        out string error)
    {
        commandId = "";
        argsJson = "";
        error = "";

        if (string.IsNullOrWhiteSpace(currentFilePath))
        {
            error = "Нет активного документа для диапазона строк.";
            return false;
        }

        var text = editorText ?? "";
        var lines = text.Split('\n');
        var lineCount = lines.Length;

        if (parsed.StartLine < 1 || parsed.EndLine < 1)
        {
            error = "Номера строк должны быть >= 1.";
            return false;
        }

        if (parsed.StartLine > parsed.EndLine)
        {
            error = "startLine должен быть <= endLine.";
            return false;
        }

        if (parsed.EndLine > lineCount)
        {
            error = $"Диапазон выходит за пределы файла: всего строк {lineCount}.";
            return false;
        }

        var resolvedCommandId = IntentMelodyAliases.TryResolveExactCommandId(parsed.Alias);
        if (string.IsNullOrWhiteSpace(resolvedCommandId))
        {
            error = $"Alias '{parsed.Alias}' не резолвится в command_id.";
            return false;
        }

        switch (resolvedCommandId)
        {
            case IdeCommands.Select:
            {
                commandId = resolvedCommandId;
                argsJson = JsonSerializer.Serialize(new
                {
                    file_path = currentFilePath,
                    start_line = parsed.StartLine,
                    start_column = 1,
                    end_line = parsed.EndLine,
                    end_column = lines[parsed.EndLine - 1].Length + 1
                });
                return true;
            }

            case IdeCommands.ApplyEdit:
            {
                var lastLine = parsed.EndLine == lineCount;
                commandId = resolvedCommandId;
                argsJson = JsonSerializer.Serialize(new
                {
                    file_path = currentFilePath,
                    start_line = parsed.StartLine,
                    start_column = 1,
                    end_line = lastLine ? parsed.EndLine : parsed.EndLine + 1,
                    end_column = lastLine ? lines[parsed.EndLine - 1].Length + 1 : 1,
                    new_text = ""
                });
                return true;
            }

            default:
                error = $"Параметрический диапазон для '{parsed.Alias}' ещё не подключён к '{resolvedCommandId}'.";
                return false;
        }
    }
}
