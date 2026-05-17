#nullable enable
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Models;
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.Services.SkiaInstruments;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

public interface ICodeNavigationMapCompositor
    : ISkiaInstrumentCompositor<CodeNavigationMapCompositionIntent, CodeNavigationMapCompositionResult>
{
    CodeNavigationMapCompositionResult Compose(
        GraphDocument doc,
        string mapLevel,
        double availableWidth,
        double availableHeight,
        CodeNavigationMapDetailLevel detailLevel = CodeNavigationMapDetailLevel.Normal);
}

public sealed record CodeNavigationMapCompositionResult(
    GraphLayoutScene LayoutScene,
    double PreferredHeight,
    IReadOnlyList<CodeNavigationMapInstrumentBlockDescriptor> CodeNavigationMapInstrumentBlocks)
{
    public CodeNavigationMapGraphSceneVm Scene =>
        CodeNavigationMapGraphSceneProjection.ToViewModel(LayoutScene);
}

public sealed record CodeNavigationMapCompositionIntent(
    GraphDocument Subgraph,
    string MapLevel,
    CodeNavigationMapDetailLevel DetailLevel = CodeNavigationMapDetailLevel.Normal);
