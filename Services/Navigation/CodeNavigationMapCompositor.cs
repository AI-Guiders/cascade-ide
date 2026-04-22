#nullable enable
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.Models;
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.Services.SkiaInstruments;

namespace CascadeIDE.Services.Navigation;

/// <summary>
/// Оркестратор компоновки <b>семантической карты</b> (граф → сцена для отрисовки): уровень карты → движок раскладки + политика высоты.
/// «Карта намерений» (control flow) — <b>CodeNavigation</b>; режим файлов/связей — <b>WorkspaceNavigation</b> (см. <see cref="Models.CodeNavigationMapLevelKind"/>).
/// Поверхность Skia compositor занимается размещением в слотах кокпита, а не смыслом узлов.
/// </summary>
public sealed class CodeNavigationMapCompositor : ICodeNavigationMapCompositor
{
    public const double DefaultWidth = CodeNavigationMapGraphPrimitives.DefaultViewportWidth;
    public const double DefaultHeightFile = CodeNavigationMapGraphPrimitives.DefaultViewportHeightFile;
    public const double DefaultHeightControlFlow = CodeNavigationMapGraphPrimitives.DefaultViewportHeightControlFlow;
    /// <summary>Верхний предел «интринсик»-высоты и слияния с viewport; сцена карты заполняет высоту области, если она передана в SkiaInstrumentViewport.</summary>
    public const double MaxHeightControlFlow = CodeNavigationMapGraphPrimitives.MaxViewportHeightControlFlow;

    private readonly ICodeNavigationMapIntentStage _intentStage;
    private readonly ICodeNavigationMapDeclutterStage _declutterStage;
    private readonly ICodeNavigationMapLayoutStage _layoutStage;

    public CodeNavigationMapCompositor(
        ICodeNavigationMapSubgraphLayoutEngine? fileLayout = null,
        ICodeNavigationMapSubgraphLayoutEngine? controlFlowLayout = null,
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
            CodeNavigationMapDetailLevel.Normal);
        var resolved = _intentStage.Resolve(context);
        var decluttered = _declutterStage.Apply(resolved);
        return WithCodeNavigationInstrumentBlocks(_layoutStage.Layout(decluttered), decluttered);
    }

    public CodeNavigationMapCompositionResult Compose(
        CodeNavigationMapSubgraphDocument doc,
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
