#nullable enable

using System.Globalization;
using System.Text.Json;

namespace CascadeIDE.Models.Editor;

/// <summary>Прямоугольный диапазон в документе (MCP/UI): файл + строки и колонки 1-based. Разбор из словаря JSON-аргументов команд <c>select</c> / <c>apply_edit</c>.</summary>
public readonly struct EditorTextSpan : IEquatable<EditorTextSpan>
{
    public EditorDocumentPath File { get; }

    public LineNumber StartLine { get; }

    public ColumnNumber StartColumn { get; }

    public LineNumber EndLine { get; }

    public ColumnNumber EndColumn { get; }

    public EditorTextSpan(
        EditorDocumentPath file,
        LineNumber startLine,
        ColumnNumber startColumn,
        LineNumber endLine,
        ColumnNumber endColumn)
    {
        File = file;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }

    /// <summary>Ожидаются ключи <c>start_line</c>, <c>start_column</c>, <c>end_line</c>, <c>end_column</c>, непустой <c>file_path</c> (каноническая нормализация).</summary>
    public static bool TryParse(IReadOnlyDictionary<string, JsonElement>? args, out EditorTextSpan span, out string error)
    {
        span = default;
        error = "";

        if (args is null)
        {
            error = "Отсутствуют аргументы.";
            return false;
        }

        var rawPath = McpCommandJsonArgs.String(args, "file_path");
        if (!EditorDocumentPath.TryCreate(rawPath, out var filePath, out error))
            return false;

        if (!tryReadLineColumn(args, "start_line", "start_column", out var sl, out var sc, out error))
            return false;
        if (!tryReadLineColumn(args, "end_line", "end_column", out var el, out var ec, out error))
            return false;

        if (!LineRange.TryCreate(sl, el, out _))
        {
            error = "end_line не может быть меньше start_line.";
            return false;
        }

        if (sl.Equals(el) && ec.Value < sc.Value)
        {
            error = "На одной строке end_column не может быть меньше start_column.";
            return false;
        }

        span = new EditorTextSpan(filePath, sl, sc, el, ec);
        return true;
    }

    public bool Equals(EditorTextSpan other) =>
        File.Equals(other.File)
        && StartLine.Equals(other.StartLine)
        && StartColumn.Equals(other.StartColumn)
        && EndLine.Equals(other.EndLine)
        && EndColumn.Equals(other.EndColumn);

    public override bool Equals(object? obj) => obj is EditorTextSpan other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(File, StartLine, StartColumn, EndLine, EndColumn);

    public static bool operator ==(EditorTextSpan left, EditorTextSpan right) => left.Equals(right);

    public static bool operator !=(EditorTextSpan left, EditorTextSpan right) => !left.Equals(right);

    private static bool tryReadLineColumn(
        IReadOnlyDictionary<string, JsonElement> args,
        string lineKey,
        string columnKey,
        out LineNumber line,
        out ColumnNumber column,
        out string error)
    {
        line = default;
        column = default;
        error = "";

        var lineRawOpt = readRequiredInt(args, lineKey);
        var colRawOpt = readRequiredInt(args, columnKey);
        if (lineRawOpt is null)
        {
            error = string.Format(CultureInfo.InvariantCulture, "Отсутствует или некорректное числовое поле «{0}».", lineKey);
            return false;
        }

        if (colRawOpt is null)
        {
            error = string.Format(CultureInfo.InvariantCulture, "Отсутствует или некорректное числовое поле «{0}».", columnKey);
            return false;
        }

        if (!LineNumber.TryCreate(lineRawOpt.Value, out line))
        {
            error = string.Format(CultureInfo.InvariantCulture, "«{0}» должно быть ≥ {1}; получено {2}.", lineKey, LineNumber.MinimumOneBasedInclusive, lineRawOpt.Value);
            return false;
        }

        if (!ColumnNumber.TryCreate(colRawOpt.Value, out column))
        {
            error = string.Format(CultureInfo.InvariantCulture, "«{0}» должно быть ≥ {1}; получено {2}.", columnKey, ColumnNumber.MinimumOneBasedInclusive, colRawOpt.Value);
            return false;
        }

        return true;
    }

    private static int? readRequiredInt(IReadOnlyDictionary<string, JsonElement> args, string key)
    {
        if (!args.TryGetValue(key, out var e) || e.ValueKind != JsonValueKind.Number || !e.TryGetInt32(out var v))
            return null;
        return v;
    }
}

/// <summary>Разбор аргументов <c>get_editor_content_range</c>: 1-based инклюзивный диапазон строк.</summary>
public static class EditorContentLineRangeMcpArgs
{
    /// <summary>Если ключ отсутствует, используется 1 (как прежнее поведение <see cref="McpCommandJsonArgs.Int"/>).</summary>
    public static bool TryParse(IReadOnlyDictionary<string, JsonElement>? args, out LineRange lines, out string error)
    {
        lines = default;
        error = "";

        var startRaw = rawOrOne(args, "start_line");
        var endRaw = rawOrOne(args, "end_line");

        if (!LineNumber.TryCreate(startRaw, out var lnStart))
        {
            error = string.Format(CultureInfo.InvariantCulture, "start_line должно быть ≥ {0}; получено {1}.", LineNumber.MinimumOneBasedInclusive, startRaw);
            return false;
        }

        if (!LineNumber.TryCreate(endRaw, out var lnEnd))
        {
            error = string.Format(CultureInfo.InvariantCulture, "end_line должно быть ≥ {0}; получено {1}.", LineNumber.MinimumOneBasedInclusive, endRaw);
            return false;
        }

        if (!LineRange.TryCreate(lnStart, lnEnd, out lines))
        {
            error = "end_line не может быть меньше start_line.";
            return false;
        }

        return true;
    }

    private static int rawOrOne(IReadOnlyDictionary<string, JsonElement>? args, string key)
    {
        if (args is null || !args.TryGetValue(key, out var e) || e.ValueKind != JsonValueKind.Number || !e.TryGetInt32(out var v))
            return 1;
        return v;
    }
}

/// <summary>Разбор <c>go_to_position</c>: обязательны <c>file_path</c>, <c>line</c>, <c>end_line</c> опционально, <c>column</c>, <c>end_column</c>.</summary>
public static class EditorGoToPositionMcpArgs
{
    public static bool TryParse(
        IReadOnlyDictionary<string, JsonElement>? args,
        out EditorDocumentPath file,
        out LineNumber line,
        out ColumnNumber column,
        out LineNumber? endLine,
        out ColumnNumber? endColumn,
        out string error)
    {
        file = default;
        line = default;
        column = default;
        endLine = null;
        endColumn = null;
        error = "";

        if (args is null)
        {
            error = "Отсутствуют аргументы.";
            return false;
        }

        var rawPath = McpCommandJsonArgs.String(args, "file_path");
        if (!EditorDocumentPath.TryCreate(rawPath, out file, out error))
            return false;

        if (!args.TryGetValue("line", out var lineEl) || lineEl.ValueKind != JsonValueKind.Number || !lineEl.TryGetInt32(out var lineRaw))
        {
            error = "Отсутствует или некорректное поле «line».";
            return false;
        }

        if (!args.TryGetValue("column", out var colEl) || colEl.ValueKind != JsonValueKind.Number || !colEl.TryGetInt32(out var colRaw))
        {
            error = "Отсутствует или некорректное поле «column».";
            return false;
        }

        if (!LineNumber.TryCreate(lineRaw, out line))
        {
            error = string.Format(CultureInfo.InvariantCulture, "line должно быть ≥ {0}; получено {1}.", LineNumber.MinimumOneBasedInclusive, lineRaw);
            return false;
        }

        if (!ColumnNumber.TryCreate(colRaw, out column))
        {
            error = string.Format(CultureInfo.InvariantCulture, "column должно быть ≥ {0}; получено {1}.", ColumnNumber.MinimumOneBasedInclusive, colRaw);
            return false;
        }

        var elOpt = McpCommandJsonArgs.OptionalInt32(args, "end_line");
        var ecOpt = McpCommandJsonArgs.OptionalInt32(args, "end_column");
        if (elOpt.HasValue)
        {
            if (!LineNumber.TryCreate(elOpt.Value, out var ln))
            {
                error = string.Format(CultureInfo.InvariantCulture, "end_line должно быть ≥ {0}; получено {1}.", LineNumber.MinimumOneBasedInclusive, elOpt.Value);
                return false;
            }

            endLine = ln;
        }

        if (ecOpt.HasValue)
        {
            if (!ColumnNumber.TryCreate(ecOpt.Value, out var cn))
            {
                error = string.Format(CultureInfo.InvariantCulture, "end_column должно быть ≥ {0}; получено {1}.", ColumnNumber.MinimumOneBasedInclusive, ecOpt.Value);
                return false;
            }

            endColumn = cn;
        }

        return true;
    }
}

/// <summary>Запрос <c>reveal_editor_range</c> (ADR 0130): строки и/или member/scope + опциональная длительность.</summary>
public readonly struct EditorRevealRangeRequest
{
    public EditorDocumentPath File { get; init; }

    public LineRange? Lines { get; init; }

    public string? MemberKey { get; init; }

    public Intercom.AttachmentSyntaxScope? SyntaxScope { get; init; }

    public int? DurationMs { get; init; }
}

/// <summary>Разбор <c>reveal_editor_range</c> (ADR 0130 фаза 2).</summary>
public static class EditorRevealRangeMcpArgs
{
    public static bool TryParse(
        IReadOnlyDictionary<string, JsonElement>? args,
        out EditorRevealRangeRequest request,
        out string error)
    {
        request = default;
        error = "";

        if (args is null)
        {
            error = "Отсутствуют аргументы.";
            return false;
        }

        if (!EditorDocumentPath.TryCreate(McpCommandJsonArgs.String(args, "file_path"), out var file, out var fileErr))
        {
            error = fileErr;
            return false;
        }

        LineRange? lines = null;
        if (args.ContainsKey("start_line") || args.ContainsKey("end_line"))
        {
            if (!EditorContentLineRangeMcpArgs.TryParse(args, out var range, out var rangeErr))
            {
                error = rangeErr;
                return false;
            }

            lines = range;
        }

        var memberKey = McpCommandJsonArgs.String(args, "member_key");
        Intercom.AttachmentSyntaxScope? syntaxScope = null;
        if (args.TryGetValue("syntax_scope", out var scopeEl))
            Intercom.AttachmentSyntaxScope.TryParse(scopeEl, out syntaxScope);

        var durationMs = McpCommandJsonArgs.OptionalInt32(args, "duration_ms");

        if (lines is null && string.IsNullOrWhiteSpace(memberKey) && syntaxScope is null)
        {
            error = "Нужен диапазон строк (start_line, end_line) или member_key / syntax_scope.";
            return false;
        }

        request = new EditorRevealRangeRequest
        {
            File = file,
            Lines = lines,
            MemberKey = string.IsNullOrWhiteSpace(memberKey) ? null : memberKey.Trim(),
            SyntaxScope = syntaxScope,
            DurationMs = durationMs,
        };
        return true;
    }
}
