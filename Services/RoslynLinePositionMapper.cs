using Microsoft.CodeAnalysis.Text;

using CascadeIDE.Models.Editor;

namespace CascadeIDE.Services;

/// <summary>
/// Преобразование позиции диагностик Roslyn в 1-based координаты редактора/MCP.<br/>
/// Roslyn <see cref="LinePosition"/> — <b>0-based</b> по строке и символу (UTF-16); JSON команд IDE — <b>1-based</b>.
/// </summary>
public static class RoslynLinePositionMapper
{
    public static LineNumber ToEditorLineNumber(LinePosition roslynZeroBased) =>
        LineNumber.TryCreate(roslynZeroBased.Line + 1, out var ln) ? ln : default;

    public static ColumnNumber ToEditorColumnNumber(LinePosition roslynZeroBased) =>
        ColumnNumber.TryCreate(roslynZeroBased.Character + 1, out var cn) ? cn : default;

    /// <summary>Оба компонента 1-based.</summary>
    public static (LineNumber Line, ColumnNumber Column) ToEditorLineColumn(LinePosition roslynZeroBased) =>
        (ToEditorLineNumber(roslynZeroBased), ToEditorColumnNumber(roslynZeroBased));
}
