using CascadeIDE.Cockpit.Composition.Shell;

using CascadeIDE.Features.UiChrome;

using CascadeIDE.Services.Presentation;



namespace CascadeIDE.ViewModels;



/// <summary>Compact tier IDE-scan presentation (ADR 0171 §2.3): Forward center, PFD/Intercom right or bottom, MFD bottom.</summary>

public partial class MainWindowViewModel

{

    private MainWindowShellSurfaceComposition CompactShellSurface => ShellSurfaceComposition;



    private bool IsCompactIntercomInForward =>

        IsCompactPresentationTier && IsForwardIntercomHostVisible;



    /// <summary>Колонка MFD (cockpit): <c>MfdShellView</c> справа. В compact — false.</summary>

    public bool IsCockpitMfdColumnVisible =>

        !IsCompactPresentationTier && CompactShellSurface.MfdColumnVisibleInMainGrid;



    /// <summary>Колонка PFD (cockpit): слева. В compact — false (PFD на правой колонке).</summary>

    public bool IsCockpitPfdColumnVisible =>

        !IsCompactPresentationTier && CompactShellSurface.PfdSurfaceVisible;



    /// <summary>Правая колонка compact: Intercom aux и/или PFD (Solution Explorer).</summary>

    public bool IsCompactRightChromeColumnVisible =>

        IsCompactPresentationTier

        && !IsCompactIntercomInForward

        && CompactShellSurface.CompactRightChromeColumnVisible;



    public bool IsCompactIntercomAuxVisible =>

        IsCompactPresentationTier

        && !IsCompactIntercomInForward

        && CompactShellSurface.IntercomAuxColumnVisible;



    public bool IsCompactIntercomBottomDockVisible =>

        IsCompactPresentationTier

        && !IsCompactIntercomInForward

        && CompactShellSurface.IntercomBottomDockVisible;



    public bool IsCompactPfdRightVisible =>

        IsCompactPresentationTier

        && !IsCompactIntercomInForward

        && CompactShellSurface.PfdRightColumnVisible;



    /// <summary>Нижний dock MFD в compact: terminal, build, git, problems.</summary>

    public bool IsCompactMfdBottomDockVisible =>

        IsCompactPresentationTier && IsMfdContourContentVisible;



    public int CompactRightChromeColumnPixelWidth =>

        IsCompactPresentationTier && !IsCompactIntercomInForward

            ? CompactShellSurface.CompactRightChromeColumnPixelWidth

            : 0;



    public int CompactIntercomBottomDockHeightPixels =>

        IsCompactIntercomBottomDockVisible

            ? Math.Max(

                UiWorkspaceLayoutRuntimeMetrics.BottomPanelMinRowPixels,

                CompactShellSurface.IntercomBottomDockHeightPx)

            : 0;



    public int CompactMfdBottomDockHeightPixels =>

        IsCompactMfdBottomDockVisible

            ? Math.Max(

                UiWorkspaceLayoutRuntimeMetrics.BottomPanelMinRowPixels,

                CompactShellSurface.MfdBottomDockHeightPx)

            : 0;



    /// <summary>Строка <c>MainGrid</c> для нижнего MFD (4 при одном dock, 6 при Intercom+MFD).</summary>

    public int CompactMfdBottomDockGridRow =>

        IsCompactIntercomBottomDockVisible ? 6 : 4;



    public int CompactMfdBottomDockSplitterGridRow =>

        IsCompactIntercomBottomDockVisible ? 5 : 3;



    /// <summary>Строки <c>MainGrid</c> для compact (нижние docks) vs cockpit.</summary>

    public string MainGridRowDefinitions =>

        IsCompactPresentationTier

            ? PresentationCompactMainGridLayoutBuilder.BuildRowDefinitions(

                IsCompactIntercomBottomDockVisible,

                IsCompactMfdBottomDockVisible)

            : PresentationCompactMainGridLayoutBuilder.CockpitRowDefinitions;



    public void NotifyCompactIdeLayoutChanged()

    {

        OnPropertyChanged(nameof(IsCockpitMfdColumnVisible));

        OnPropertyChanged(nameof(IsCockpitPfdColumnVisible));

        OnPropertyChanged(nameof(IsCompactRightChromeColumnVisible));

        OnPropertyChanged(nameof(IsCompactIntercomAuxVisible));

        OnPropertyChanged(nameof(IsCompactIntercomBottomDockVisible));

        OnPropertyChanged(nameof(IsCompactPfdRightVisible));

        OnPropertyChanged(nameof(IsCompactMfdBottomDockVisible));

        OnPropertyChanged(nameof(CompactRightChromeColumnPixelWidth));

        OnPropertyChanged(nameof(CompactIntercomBottomDockHeightPixels));

        OnPropertyChanged(nameof(CompactMfdBottomDockHeightPixels));

        OnPropertyChanged(nameof(CompactMfdBottomDockGridRow));

        OnPropertyChanged(nameof(CompactMfdBottomDockSplitterGridRow));

        OnPropertyChanged(nameof(MainGridRowDefinitions));

        OnPropertyChanged(nameof(IsMfdColumnVisible));

        OnPropertyChanged(nameof(IsPfdColumnVisible));

        OnPropertyChanged(nameof(ChatPanelColumnPixelWidth));

        OnPropertyChanged(nameof(IsChatPanelColumnVisible));

    }

}


