#nullable enable

using System.IO;

namespace CascadeIDE.Features.Workspace.DataAcquisition;

/// <summary>Безопасное чтение текстовых файлов workspace (ADR 0102).</summary>
public static class WorkspaceTextFileReader
{
    public static bool TryReadAllText(string absolutePath, out string content)
    {
        content = "";
        if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            return false;

        try
        {
            content = File.ReadAllText(absolutePath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
