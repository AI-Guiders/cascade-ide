#nullable enable
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Узел control-flow в полоске HUD (те же глифы/фигуры, что на мини-карте и в gutter).</summary>
public sealed partial class EditorHudControlFlowNodeVm : ObservableObject
{
    [ObservableProperty]
    private bool _isHighlighted;

    public required string NodeId { get; init; }
    public required string Kind { get; init; }
    public required string FullPath { get; init; }
    public int? LineStart { get; init; }
    public int? LineEnd { get; init; }
    public string? LegendLine { get; init; }
    public string Label { get; init; } = "";

    /// <summary>Текст внутри узла (пусто у exit — там стрелка).</summary>
    public string Glyph { get; init; } = "";

    public ControlFlowNodeVisualKind VisualKind { get; init; }
    public bool ShowExitArrow { get; init; }
    public string? HudToolTip { get; init; }

    public bool HasTextGlyph => !string.IsNullOrEmpty(Glyph);

    public bool HudGlyphInkAnchor => HasTextGlyph && IsVisualAnchor;

    public bool HudGlyphInkDefault => HasTextGlyph && !IsVisualAnchor;

    public bool IsVisualAnchor => VisualKind == ControlFlowNodeVisualKind.Anchor;
    public bool IsVisualCircle => VisualKind == ControlFlowNodeVisualKind.Circle;
    public bool IsVisualDiamond => VisualKind == ControlFlowNodeVisualKind.Diamond;
    public bool IsVisualExit => VisualKind == ControlFlowNodeVisualKind.Exit;

    public CodeNavigationMapNodeNavigatePayload ToNavigatePayload() =>
        new(FullPath, LineStart, LineEnd, LegendLine, Kind);

    public bool IsCaretOnNode(int caretLineOneBased)
    {
        if (caretLineOneBased < 1)
            return false;
        if (LineStart is not int s)
            return false;
        var e = LineEnd ?? s;
        return caretLineOneBased >= s && caretLineOneBased <= Math.Max(s, e);
    }
}
