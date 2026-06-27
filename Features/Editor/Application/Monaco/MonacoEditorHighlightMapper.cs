using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Features.Editor.Application.Monaco;

public static class MonacoEditorHighlightMapper
{
    public static IReadOnlyList<CideEditorDecoration> ToDecorations(IReadOnlyList<TextSpan> spans)
    {
        if (spans.Count == 0)
            return Array.Empty<CideEditorDecoration>();

        var list = new List<CideEditorDecoration>(spans.Count);
        foreach (var span in spans)
            list.Add(new CideEditorDecoration(span.Start, span.Length, "reference-highlight", null));

        return list;
    }
}
