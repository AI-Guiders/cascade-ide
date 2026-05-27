#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CascadeIDE.Contracts;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Визуальный язык узла CF совпадает с <see cref="Views.SkiaKit.Graph.SkiaGraphSceneDrawing"/> BuildNodeGlyph / фигуры.</summary>
public enum ControlFlowNodeVisualKind
{
    Anchor,
    Circle,
    Diamond,
    Exit
}

/// <summary>Одна метка в gutter / virtual spacing lane: логическая строка 1-based.</summary>
public sealed record ControlFlowLineVisual(
    int LineOneBased,
    ControlFlowNodeVisualKind VisualKind,
    string NodeKind,
    string TextGlyph,
    bool ShowExitArrow,
    string? ToolTip);

/// <summary>Сборка глифов control-flow для редактора (без дублирования логики Skia).</summary>
[ComputingUnit]
public static class CodeNavigationControlFlowGlyphComposer
{
    private const string ExitStepKind = "exit_step";
    private const string ConditionStepKind = "condition_step";

    public static ControlFlowNodeVisualKind ResolveVisualKind(CodeNavigationMapGraphNodeLayout n)
    {
        if (n.IsAnchor)
            return ControlFlowNodeVisualKind.Anchor;
        if (IsExitNode(n))
            return ControlFlowNodeVisualKind.Exit;
        if (IsConditionNode(n) || n.Shape == CodeNavigationMapNodeShape.Condition)
            return ControlFlowNodeVisualKind.Diamond;
        return ControlFlowNodeVisualKind.Circle;
    }

    /// <summary>Текст внутри узла (пусто для exit — стрелка рисуется отдельно).</summary>
    public static (string TextGlyph, bool ShowExitArrow) BuildTextGlyph(
        CodeNavigationMapGraphNodeLayout n,
        CodeNavigationMapGraphSceneVm scene)
    {
        var useLegend = scene.UseLegendColumn;
        var showGlyphs = scene.ShowNodeLegendGlyphs;

        if (n.IsAnchor)
            return ("A", false);
        if (string.Equals(n.Kind, "protected_step", StringComparison.OrdinalIgnoreCase))
            return ("T", false);
        if (IsExitNode(n))
            return ("", true);
        if (n.LegendIndex is int idxLegend && idxLegend > 0 && (useLegend || showGlyphs))
            return (idxLegend.ToString(CultureInfo.InvariantCulture), false);
        if (IsConditionNode(n) || n.Shape == CodeNavigationMapNodeShape.Condition)
            return ("?", false);
        if (IsHandlerNode(n))
            return ("!", false);
        return ("•", false);
    }

    public static void GetNodeVisual(
        CodeNavigationMapGraphNodeLayout n,
        CodeNavigationMapGraphSceneVm scene,
        out ControlFlowNodeVisualKind kind,
        out string textGlyph,
        out bool showExitArrow)
    {
        kind = ResolveVisualKind(n);
        (textGlyph, showExitArrow) = BuildTextGlyph(n, scene);
    }

    /// <summary>Строка для gutter: <see cref="LineStart"/>, иначе для якоря без строки — первая строка файла.</summary>
    public static int? TryGetGutterLine(CodeNavigationMapGraphNodeLayout n)
    {
        if (n.LineStart is int ls and > 0)
            return ls;
        if (n.IsAnchor)
            return 1;
        return null;
    }

    public static IReadOnlyList<ControlFlowLineVisual> BuildGutterLineVisuals(CodeNavigationMapGraphSceneVm scene)
    {
        var byLine = new Dictionary<int, ControlFlowLineVisual>();

        foreach (var n in scene.Nodes)
        {
            var line = TryGetGutterLine(n);
            if (line is null)
                continue;
            GetNodeVisual(n, scene, out var kind, out var text, out var arrow);
            var tip = TooltipForNode(n);
            byLine[line.Value] = new ControlFlowLineVisual(line.Value, kind, n.Kind, text, arrow, tip);
        }

        return byLine.Keys.OrderBy(k => k).Select(k => byLine[k]).ToList();
    }

    /// <summary>Текст для ToolTip узла на HUD глифах.</summary>
    public static string? TooltipForNode(CodeNavigationMapGraphNodeLayout n)
    {
        if (!string.IsNullOrWhiteSpace(n.LegendLine))
            return n.LegendLine.Trim();
        if (!string.IsNullOrWhiteSpace(n.Label))
            return n.Label.Trim();
        return null;
    }

    private static bool IsExitNode(CodeNavigationMapGraphNodeLayout n) =>
        string.Equals(n.Kind, ExitStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsConditionNode(CodeNavigationMapGraphNodeLayout n) =>
        string.Equals(n.Kind, ConditionStepKind, StringComparison.OrdinalIgnoreCase);

    private static bool IsHandlerNode(CodeNavigationMapGraphNodeLayout n) =>
        string.Equals(n.Kind, "handler_step", StringComparison.OrdinalIgnoreCase);
}
