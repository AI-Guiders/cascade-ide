using CascadeIDE.Services;

namespace CascadeIDE.Features.Editor;

/// <summary>Inline hints редактора: фильтрация и выдача по настройкам <c>editor.inline_hints</c>.</summary>
public sealed partial class EditorWorkspaceViewModel
{
    /// <summary>Собрать inlay hints для документа с учётом пользовательских настроек editor.inline_hints.</summary>
    public IReadOnlyList<EditorTrailingInlayPart> GetEditorInlineHintsForFile(string filePath, string sourceText)
    {
        var opts = _host.McpSettings.Editor.InlineHints;
        if (!opts.Enabled)
            return [];

        var parts = _host.HostCsharpLanguageService.GetVarInlayHintsForFile(filePath, sourceText);
        if (opts.ParameterNames && opts.VariableTypes)
            return parts;

        var filtered = parts.Where(static p => !string.IsNullOrWhiteSpace(p.Label));

        if (!opts.ParameterNames)
            filtered = filtered.Where(static p => !p.Label.TrimEnd().EndsWith(':'));
        if (!opts.VariableTypes)
            filtered = filtered.Where(static p => !p.Label.StartsWith("  ", StringComparison.Ordinal));

        return filtered.ToList();
    }
}
