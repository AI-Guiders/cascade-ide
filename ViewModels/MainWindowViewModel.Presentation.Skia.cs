using CascadeIDE.Cockpit.Composition;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Features.Shell.Application;

namespace CascadeIDE.ViewModels;

/// <summary>Skia zone-geometry overlay + instrument mount styles (Presentation slice).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Включён debug-overlay контуров зон (ручная валидация геометрии W2).</summary>
    public bool ShowSkiaZoneGeometryOverlay => _settings.Display.Skia.ZoneGeometryOverlay;

    public bool IsSkiaZoneGeometryOverlayPfdVisible =>
        MainWindowPresentationCapabilitiesProjection.IsSkiaZoneGeometryOverlayPfdVisible(
            ShowSkiaZoneGeometryOverlay,
            IsPfdColumnVisible);

    public bool IsSkiaZoneGeometryOverlayForwardVisible =>
        MainWindowPresentationCapabilitiesProjection.IsSkiaZoneGeometryOverlayForwardVisible(
            ShowSkiaZoneGeometryOverlay);

    public bool IsSkiaZoneGeometryOverlayMfdVisible =>
        MainWindowPresentationCapabilitiesProjection.IsSkiaZoneGeometryOverlayMfdVisible(
            ShowSkiaZoneGeometryOverlay,
            IsMfdColumnVisible);

    /// <summary>Wave 3: включить отрисовку инструмента в Skia mount-слое зон P/F/M.</summary>
    public bool UseSkiaInstrumentMount => _settings.Display.Skia.InstrumentMount;

    /// <summary>Декларативный mount-style mount-инструмента (идёт из <c>[display.mount]</c>).</summary>
    public string InstrumentMountStyle =>
        MainWindowPresentationSurfaceProjection.InstrumentMountDisplayStyle(_settings.Display);

    /// <summary>Резолв style для mount в слоте PFD с учётом registry-правил.</summary>
    public string PfdInstrumentMountStyle =>
        MainWindowPresentationSurfaceProjection.ResolveInstrumentMountStyleForSlot(
            _instrumentMountPolicyResolver,
            _settings.Display,
            ActiveAttentionLayoutSurface,
            "pfd",
            CockpitStandardInstrumentIds.IdeHealthStatusV1);

    /// <summary>Резолв style для mount в слоте MFD с учётом registry-правил.</summary>
    public string MfdInstrumentMountStyle =>
        MainWindowPresentationSurfaceProjection.ResolveInstrumentMountStyleForSlot(
            _instrumentMountPolicyResolver,
            _settings.Display,
            ActiveAttentionLayoutSurface,
            "mfd",
            CockpitStandardInstrumentIds.IdeHealthStatusV1);
}
