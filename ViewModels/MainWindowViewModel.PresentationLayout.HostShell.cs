namespace CascadeIDE.ViewModels;

/// <summary>События «окно-хост открыло полный контур» — скрытие колонок в main (<see cref="MainWindowViewModel.PresentationLayout"/>).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Свойства, зависящие от подавления колонок PFD/MFD в main при открытых окнах-хостах (ADR 0017).</summary>
    private static readonly string[] HostShellOpenInvalidatedPropertyNames =
    [
        nameof(IsMfdHostWindowShellOpen),
        nameof(IsPfdHostWindowShellOpen),
        nameof(IsPfdColumnVisible),
        nameof(IsMfdColumnVisible),
        nameof(IsSkiaZoneGeometryOverlayPfdVisible),
        nameof(IsSkiaZoneGeometryOverlayMfdVisible),
        nameof(MfdRegionPixelWidth),
        nameof(IsMfdRegionVisible),
        nameof(ActiveAttentionLayoutSurface),
        nameof(MainGridColumnDefinitions),
        nameof(IsPfdIdeHealthMountVisible),
        nameof(IsMfdIdeHealthMountVisible),
        nameof(IsMfdHostWindowIdeHealthMountVisible),
        nameof(IsPfdHostWindowIdeHealthMountVisible),
        nameof(PfdIdeHealthMountContext),
        nameof(MfdIdeHealthMountContext),
        nameof(PfdInstrumentMountStyle),
        nameof(MfdInstrumentMountStyle),
    ];

    /// <summary>
    /// Окно-хост зоны Mfd показывает <c>MfdShellView</c> (чат, терминал, обозреватель решения и т.д.) — скрываем колонку Mfd в главном окне, чтобы не дублировать контур.
    /// </summary>
    public void SetMfdHostWindowShellOpen(bool isOpen)
    {
        if (_suppressMfdColumnForMfdHostWindow == isOpen)
            return;

        _suppressMfdColumnForMfdHostWindow = isOpen;
        foreach (var name in HostShellOpenInvalidatedPropertyNames)
            OnPropertyChanged(name);
    }

    /// <summary>Окно-хост зоны Pfd показывает дерево/semantic map — скрываем колонку Pfd в главном окне.</summary>
    public void SetPfdHostWindowShellOpen(bool isOpen)
    {
        if (_suppressPfdColumnForPfdHostWindow == isOpen)
            return;

        _suppressPfdColumnForPfdHostWindow = isOpen;
        foreach (var name in HostShellOpenInvalidatedPropertyNames)
            OnPropertyChanged(name);
    }
}
