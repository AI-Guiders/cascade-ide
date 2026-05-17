#nullable enable
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Models;
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.Services.SkiaInstruments;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Композитор graph-backed карты намерений (Intent → Declutter → Layout), ADR 0055 / 0115.</summary>
public sealed class CodeNavigationMapCompositor : ICodeNavigationMapCompositor
{
    public const double DefaultWidth = GraphViewportMetrics.DefaultWidth;
    public const double DefaultHeightFile = GraphViewportMetrics.DefaultHeightFile;
    public const double DefaultHeightControlFlow = GraphViewportMetrics.DefaultHeightControlFlow;
    public const double MaxHeightControlFlow = GraphViewportMetrics.MaxHeightControlFlow;

    private readonly ICodeNavigationMapIntentStage _intentStage;
    private readonly ICodeNavigationMapDeclutterStage _declutterStage;
    private readonly ICodeNavigationMapLayoutStage _layoutStage;

    public CodeNavigationMapCompositor(
        IGraphLayoutEngine? fileLayout = null,
        IGraphLayoutEngine? controlFlowLayout = null,
        ICodeNavigationMapIntentStage? intentStage = null,
        ICodeNavigationMapDeclutterStage? declutterStage = null,
        ICodeNavigationMapLayoutStage? layoutStage = null)
    {
        _intentStage = intentStage ?? new CodeNavigationMapIntentStage();
        _declutterStage = declutterStage ?? new CodeNavigationMapDeclutterStage(_intentStage);
        _layoutStage = layoutStage ?? new CodeNavigationMapLayoutStage(fileLayout, controlFlowLayout);
    }

    public CodeNavigationMapCompositionResult Compose(CodeNavigationMapCompositionIntent intent, in SkiaInstrumentViewport viewport)
    {
        var context = new CodeNavigationMapPipelineContext(
            intent.Subgraph,
            intent.MapLevel,
            viewport,
            intent.DetailLevel);
        var resolved = _intentStage.Resolve(context);
        var decluttered = _declutterStage.Apply(resolved);
        return WithCodeNavigationInstrumentBlocks(_layoutStage.Layout(decluttered), decluttered);
    }

    public CodeNavigationMapCompositionResult Compose(
        GraphDocument doc,
        string mapLevel,
        double availableWidth,
        double availableHeight,
        CodeNavigationMapDetailLevel detailLevel = CodeNavigationMapDetailLevel.Normal)
    {
        var context = new CodeNavigationMapPipelineContext(
            doc,
            mapLevel,
            new SkiaInstrumentViewport(availableWidth, availableHeight),
            detailLevel);
        var resolved = _intentStage.Resolve(context);
        var decluttered = _declutterStage.Apply(resolved);
        return WithCodeNavigationInstrumentBlocks(_layoutStage.Layout(decluttered), decluttered);
    }

    private static CodeNavigationMapCompositionResult WithCodeNavigationInstrumentBlocks(
        CodeNavigationMapCompositionResult laidOut,
        in CodeNavigationMapPipelineState state)
    {
        var vw = state.Viewport.Width > 0 ? state.Viewport.Width : DefaultWidth;
        var vh = laidOut.PreferredHeight;
        var blocks = CodeNavigationMapInstrumentBlockCompositor.Compose(laidOut.Scene, vw, vh);
        return laidOut with { CodeNavigationMapInstrumentBlocks = blocks };
    }
}
