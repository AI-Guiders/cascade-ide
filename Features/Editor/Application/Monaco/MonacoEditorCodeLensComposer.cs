using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Maps navigation-map CFG nodes to Monaco CodeLens (ADR 0163 M9).</summary>
public static class MonacoEditorCodeLensComposer
{
    public static IReadOnlyList<CideEditorCodeLensItem> FromNavigationScene(
        string? filePath,
        CodeNavigationMapGraphSceneVm? scene)
    {
        if (string.IsNullOrWhiteSpace(filePath) || scene is null || scene.IsEmpty)
            return [];

        var list = new List<CideEditorCodeLensItem>(scene.Nodes.Count);
        foreach (var node in scene.Nodes)
        {
            if (!EditorTextCoordinateUtilities.PathsReferToSameFile(node.FullPath, filePath))
                continue;
            if (node.LineStart is not int line || line < 1)
                continue;

            var title = node.LegendIndex is int idx
                ? $"{idx}: {node.Label}"
                : node.Label;
            if (string.IsNullOrWhiteSpace(title))
                continue;

            list.Add(new CideEditorCodeLensItem(node.Id, line, 1, title.Trim()));
        }

        return list;
    }

    public static CodeNavigationMapNodeNavigatePayload? TryResolveNavigation(
        string lensId,
        CodeNavigationMapGraphSceneVm? scene)
    {
        if (string.IsNullOrWhiteSpace(lensId) || scene is null)
            return null;

        foreach (var node in scene.Nodes)
        {
            if (!string.Equals(node.Id, lensId, StringComparison.Ordinal))
                continue;
            return new CodeNavigationMapNodeNavigatePayload(
                node.FullPath,
                node.LineStart,
                node.LineEnd,
                node.LegendLine,
                node.Kind);
        }

        return null;
    }
}

public static class MonacoEditorInlayMapper
{
    public static IReadOnlyList<CideEditorInlayHint> ToHints(
        string sourceText,
        IReadOnlyList<EditorTrailingInlayPart> parts)
    {
        if (parts.Count == 0)
            return [];

        var list = new List<CideEditorInlayHint>(parts.Count);
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part.Label))
                continue;
            var (line, column) = LineColumnFromOffset(sourceText, part.AnchorOffset);
            var kind = part.Label.TrimEnd().EndsWith(':') ? "parameter" : "type";
            list.Add(new CideEditorInlayHint(line, column, part.Label, kind));
        }

        return list;
    }

    private static (int line, int column) LineColumnFromOffset(string text, int offset)
    {
        if (string.IsNullOrEmpty(text) || offset < 0)
            return (1, 1);
        offset = Math.Min(offset, text.Length);
        var line = 1;
        var lineStart = 0;
        for (var i = 0; i < offset; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        return (line, offset - lineStart + 1);
    }
}
