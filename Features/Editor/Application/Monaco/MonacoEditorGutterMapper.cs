using CascadeIDE.Features.WorkspaceNavigation.Application;

namespace CascadeIDE.Features.Editor.Application.Monaco;

public static class MonacoEditorGutterMapper
{
    public static IReadOnlyList<CideEditorGutterGlyph> ToGlyphs(IReadOnlyList<ControlFlowLineVisual>? visuals)
    {
        if (visuals is null || visuals.Count == 0)
            return Array.Empty<CideEditorGutterGlyph>();

        var list = new List<CideEditorGutterGlyph>(visuals.Count);
        foreach (var v in visuals)
        {
            if (string.IsNullOrEmpty(v.TextGlyph))
                continue;
            list.Add(new CideEditorGutterGlyph(
                v.LineOneBased,
                v.TextGlyph,
                v.ToolTip,
                v.VisualKind.ToString()));
        }

        return list;
    }
}
