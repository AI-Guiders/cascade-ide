#nullable enable
using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>
/// Сборка JSON-args для параметрики с двумя int-слотами (диапазон строк) по <c>command_id</c> из каталога.
/// Каталог не содержит <c>binder</c> (ADR 0109): соответствие «команда → форма args» живёт в коде в этом типе.
/// </summary>
internal static class ParametricLineRangeArgsBuilder
{
    public static bool TryBuild(
        ParametricIntentMelody.ParsedLineRange parsed,
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

        var startLine = parsed.Lines.Start.Value;
        var endLine = parsed.Lines.End.Value;

        if (endLine > lineCount)
        {
            error = $"Диапазон выходит за пределы файла: всего строк {lineCount}.";
            return false;
        }

        if (!IntentMelodyCatalog.TryGetRoot(parsed.Alias, out var entry) || entry.Shape != IntentMelodyShape.Parametric)
        {
            error = $"Alias '{parsed.Alias}' отсутствует в каталоге melody или не параметрический.";
            return false;
        }

        var catalogCmd = entry.CommandId.Trim();
        if (string.IsNullOrWhiteSpace(catalogCmd))
        {
            error = $"У корня '{parsed.Alias}' пустой command_id в каталоге.";
            return false;
        }

        var ts = entry.TailSignature ?? "";
        if (IntentMelodyTailSemantics.HasUrlSlot(ts)
            || IntentMelodyTailSemantics.CountDelimitedNumericSlots(ts) != 2)
        {
            error = $"Форма tail_signature для '{parsed.Alias}' ожидает ровно два числовых слота (напр. <start:ln>:<end:ln> или устар. <start:int>:<end:int>) для диапазона строк.";
            return false;
        }

        switch (catalogCmd)
        {
            case IdeCommands.Select:
                commandId = catalogCmd;
                argsJson = JsonSerializer.Serialize(new
                {
                    file_path = currentFilePath,
                    start_line = startLine,
                    start_column = 1,
                    end_line = endLine,
                    end_column = lines[endLine - 1].Length + 1,
                });
                return true;

            case IdeCommands.ApplyEdit:
                var lastLine = endLine == lineCount;

                commandId = catalogCmd;
                argsJson = JsonSerializer.Serialize(new
                {
                    file_path = currentFilePath,
                    start_line = startLine,
                    start_column = 1,
                    end_line = lastLine ? endLine : endLine + 1,
                    end_column = lastLine ? lines[endLine - 1].Length + 1 : 1,
                    new_text = "",
                });
                return true;

            default:
                error = $"Параметрический диапазон строк для '{parsed.Alias}' (command_id '{catalogCmd}') ещё не подключён к сборке args.";
                return false;
        }
    }
}
