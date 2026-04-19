namespace CascadeIDE.Services;

/// <summary>
/// Чистые функции для MCP/редактора: перевод 1-based line/column в offset в тексте с разделителем <c>\n</c>
/// и сравнение путей к файлам. Единая реализация вместо копий в View и VM (план B в architecture-migration).
/// </summary>
public static class EditorTextCoordinateUtilities
{
    /// <summary>
    /// Переводит позицию (строка и колонка с 1) в смещение в символах. Разделитель строк — только <c>\n</c>.
    /// Возвращает -1 при некорректных координатах.
    /// </summary>
    public static int LineColumnToOffset(string text, int line, int column)
    {
        if (line < 1 || column < 1)
            return -1;

        var lines = text.Split('\n');
        if (line > lines.Length)
            return -1;

        int offset = 0;
        for (int i = 0; i < line - 1; i++)
            offset += lines[i].Length + 1;

        int lineLen = lines[line - 1].Length;
        int col = Math.Min(column, lineLen + 1);
        return offset + (col - 1);
    }

    /// <summary>
    /// Сравнивает пути к одному и тому же файлу (нормализация через <see cref="Path.GetFullPath"/> при возможности).
    /// </summary>
    public static bool PathsReferToSameFile(string a, string b)
    {
        try
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
