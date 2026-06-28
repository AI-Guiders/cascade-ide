namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Какой текст показывать во вкладке Monaco: свой документ vs общий EditorText VM.</summary>
public static class DockDocumentMonacoTextResolver
{
    public static string Resolve(
        bool isActive,
        string? vmCurrentFilePath,
        string? vmEditorText,
        string? tabFilePath,
        string? tabDocumentContent)
    {
        var docText = tabDocumentContent ?? "";
        if (!isActive)
            return docText;

        if (!string.Equals(vmCurrentFilePath, tabFilePath, StringComparison.OrdinalIgnoreCase))
            return docText;

        return vmEditorText ?? docText;
    }
}
