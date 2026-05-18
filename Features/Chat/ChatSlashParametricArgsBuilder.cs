#nullable enable
using System.Text.Json;
using CascadeIDE.Models.Editor;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Slash-хвост → JSON-args для любой параметрической команды из каталога (wire_class из TOML).
/// Паритет с <see cref="ParametricIntentMelody.TryResolveParametricExecution"/> для melody <c>c:</c>.
/// </summary>
internal static class ChatSlashParametricArgsBuilder
{
    public static bool IsParametricCatalogCommand(string commandId) =>
        IntentMelodyCatalog.TryGetParametricRootByCommandId(commandId, out _);

    public static bool TryBuild(
        string commandId,
        string? argsTail,
        ChatSlashEditorContext editor,
        out IReadOnlyDictionary<string, JsonElement>? args,
        out string error)
    {
        args = null;
        error = "";

        if (!IntentMelodyCatalog.TryGetParametricRootByCommandId(commandId, out var root))
        {
            error = "Команда не параметрическая в каталоге melody.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(root.WireClass)
            || !IntentMelodyCatalog.TryGetTailWireClass(root.WireClass, out var wire))
        {
            error = $"У параметрической команды «{commandId}» не задан wire_class в каталоге.";
            return false;
        }

        return wire.Kind switch
        {
            TailWireKind.DelimitedSlots when IntentMelodyTailSemantics.CountDelimitedNumericSlots(root.TailSignature) == 2
                => TryBuildLineRange(root, argsTail, editor, out args, out error),
            TailWireKind.SingleRemainder when IntentMelodyTailSemantics.HasUrlSlot(root.TailSignature)
                => TryBuildUrlRemainder(argsTail, out args, out error),
            _ => Fail($"Форма wire_class «{root.WireClass}» для slash ещё не поддержана.", out args, out error),
        };
    }

    public static bool RequiresNonEmptyArgsTail(MelodyRootEntry root)
    {
        if (root.Shape != IntentMelodyShape.Parametric)
            return false;

        if (string.IsNullOrWhiteSpace(root.WireClass)
            || !IntentMelodyCatalog.TryGetTailWireClass(root.WireClass, out var wire))
            return true;

        return wire.Kind switch
        {
            TailWireKind.DelimitedSlots => true,
            TailWireKind.SingleRemainder when IntentMelodyTailSemantics.HasUrlSlot(root.TailSignature) => false,
            _ => true,
        };
    }

    private static bool TryBuildLineRange(
        MelodyRootEntry root,
        string? argsTail,
        ChatSlashEditorContext editor,
        out IReadOnlyDictionary<string, JsonElement>? args,
        out string error)
    {
        args = null;
        if (!TryParseLineRangeTail(argsTail, out var start, out var end, out error))
            return false;

        if (!LineNumber.TryCreate(start, out var lnStart)
            || !LineNumber.TryCreate(end, out var lnEnd)
            || !LineRange.TryCreate(lnStart, lnEnd, out var lines))
        {
            error = "Номера строк должны быть ≥ 1, конец — не раньше начала.";
            return false;
        }

        var displayTail = start == end ? $"{root.Slug}:{start}" : $"{root.Slug}:{start}:{end}";
        var parsed = new ParametricIntentMelody.ParsedLineRange(root.Slug, displayTail, lines);

        if (!ParametricLineRangeArgsBuilder.TryBuild(
                parsed,
                editor.CurrentFilePath,
                editor.EditorText ?? "",
                out _,
                out var argsJson,
                out error))
        {
            return false;
        }

        args = JsonArgsToDictionary(argsJson);
        return true;
    }

    private static bool TryBuildUrlRemainder(
        string? argsTail,
        out IReadOnlyDictionary<string, JsonElement>? args,
        out string error)
    {
        args = null;
        error = "";
        var url = (argsTail ?? "").Trim();
        if (url.Length == 0)
        {
            args = null;
            return true;
        }

        var json = JsonSerializer.Serialize(new { url });
        args = JsonArgsToDictionary(json);
        return true;
    }

    internal static bool TryParseLineRangeTail(string? argsTail, out int startLine, out int endLine, out string error)
    {
        startLine = 0;
        endLine = 0;
        error = "";

        var tail = (argsTail ?? "").Trim();
        if (tail.Length == 0)
        {
            error = "Укажи номера строк (1-based): одну, две через пробел или start:end.";
            return false;
        }

        var normalized = tail.Replace(':', ' ').Replace(';', ' ');
        var parts = normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 2)
        {
            error = "Ожидается одна строка или диапазон: «5», «5 10» или «5:10».";
            return false;
        }

        if (!int.TryParse(parts[0], out startLine) || startLine < 1)
        {
            error = $"Некорректный номер строки «{parts[0]}».";
            return false;
        }

        if (parts.Length == 1)
        {
            endLine = startLine;
            return true;
        }

        if (!int.TryParse(parts[1], out endLine) || endLine < 1)
        {
            error = $"Некорректный номер строки «{parts[1]}».";
            return false;
        }

        if (endLine < startLine)
        {
            error = "Конец диапазона не может быть меньше начала.";
            return false;
        }

        return true;
    }

    private static IReadOnlyDictionary<string, JsonElement> JsonArgsToDictionary(string argsJson)
    {
        using var doc = JsonDocument.Parse(argsJson);
        return doc.RootElement.EnumerateObject()
            .ToDictionary(static p => p.Name, static p => p.Value.Clone(), StringComparer.Ordinal);
    }

    private static bool Fail(string message, out IReadOnlyDictionary<string, JsonElement>? args, out string error)
    {
        args = null;
        error = message;
        return false;
    }
}
