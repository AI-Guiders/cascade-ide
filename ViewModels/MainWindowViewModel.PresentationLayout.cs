using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.ViewModels;

/// <summary>ADR 0017: строка <c>presentation</c> и второй <c>TopLevel</c> — <see cref="Views.MfdHostWindow"/> с полным вторичным контуром (п. 8).</summary>
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

    /// <summary>Строка <c>presentation</c> с учётом оверлеев — та же, что уходит в <see cref="PresentationParse"/>.</summary>
    public string EffectivePresentationLine => _settings.GetEffectivePresentationLine();

    /// <summary>Успешный разбор <see cref="CascadeIdeSettings.GetEffectivePresentationLine"/> (может быть пустой список экранов).</summary>
    public PresentationParseResult PresentationParse => _presentationParse;

    /// <summary>
    /// Колонки рабочей строки <c>MainGrid</c> (строка для Avalonia <c>ColumnDefinitions</c>).
    /// При заданных весах якорей в <c>presentation</c> — доли <c>*</c> по ADR 0017; иначе дефолт <see cref="PresentationMainGridColumnDefinitions.Default"/>.
    /// </summary>
    public string MainGridColumnDefinitions => MainGridLayoutFrame.ColumnDefinitions;

    /// <summary>Кадр геометрии P/F/M для строки <c>MainGrid</c> (v1: колонка, число зон, нормализованные доли).</summary>
    public PresentationMainGridLayoutFrame MainGridLayoutFrame =>
        PresentationMainGridLayoutFrameBuilder.Build(
            _presentationParse,
            _presentationDedicatedMfdSecondScreen,
            _suppressMfdColumnForMfdHostWindow,
            _presentationTripleOneAnchorPerZone,
            _suppressPfdColumnForPfdHostWindow,
            PresentationLayoutAnalyzer.GetMainWindowPresentationScreenIndexOrDefault(_presentationParse));

    /// <summary>
    /// Пресет требует развернуть главное окно на весь экран при старте — см.
    /// <see cref="PresentationLayoutAnalyzer.ShouldMaximizeMainWindowAtStartup"/>.
    /// </summary>
    public bool PresentationRequestsMainWindowMaximized =>
        _presentationParse.IsSuccess
        && PresentationLayoutAnalyzer.ShouldMaximizeMainWindowAtStartup(_presentationParse.Screens);

    /// <summary>Пресет «первый экран — PFD+Forward без MFD, второй — только MFD».</summary>
    public bool PresentationRequestsDedicatedMfdSecondScreen => _presentationDedicatedMfdSecondScreen;

    /// <summary>Три дисплея: по одному якорю — <c>(P) (F) (M)</c> в любом порядке групп (ADR 0017).</summary>
    public bool PresentationRequestsTriplePfdForwardMfd => _presentationTripleOneAnchorPerZone;

    /// <summary>Пресет «три экрана по одной зоне» — требуются отдельные хосты PFD и MFD вместе с main (ADR 0017).</summary>
    public bool PresentationRequestsPfdHostWindow => _presentationTripleOneAnchorPerZone;

    /// <summary>Пресет с выносом MFD на отдельный <c>TopLevel</c> (два или три дисплея по строке <c>presentation</c>).</summary>
    public bool PresentationRequestsMfdHostWindow => _presentationMfdHostTopology;

    /// <summary>Пресет <c>(xP+yM)(F)</c> / <c>(F)(xP+yM)</c> — отдельное окно сплита P+M (ADR 0017).</summary>
    public bool PresentationRequestsPmSplitHostWindow => _presentationPmHostTopology;

    /// <summary>
    /// Индекс дисплея для окна сплита P+M в порядке <see cref="PresentationMonitorTopology.OrderScreensForPresentation"/>.
    /// </summary>
    public int? PmSplitHostPresentationScreenIndex =>
        _presentationParse.IsSuccess
        && PresentationLayoutAnalyzer.TryGetPmSplitHostPresentationScreenIndex(_presentationParse.Screens, out var idx)
            ? idx
            : null;

    /// <summary>Индекс группы <c>presentation</c> для главного окна (лобовое); для <c>(xP+yM)(F)</c> — экран с <c>F</c>.</summary>
    public int MainWindowPresentationScreenIndex =>
        PresentationLayoutAnalyzer.GetMainWindowPresentationScreenIndexOrDefault(_presentationParse);

    /// <summary>Колонки <c>Grid</c> окна сплита P+M (строка для <c>ColumnDefinitions.Parse</c>).</summary>
    public string PmSplitHostColumnDefinitions =>
        _presentationParse.IsSuccess && PmSplitHostPresentationScreenIndex is int pmIdx
            ? PresentationPmSplitHostColumnBuilder.Build(_presentationParse, pmIdx)
            : PresentationPmSplitHostColumnBuilder.Build(_presentationParse, 0);

    /// <summary>Строка <c>presentation</c> задаёт <c>(xP+yM)(F)</c> — выровнять главное окно по экрану с <c>F</c> (см. <see cref="MainWindowPresentationScreenIndex"/>).</summary>
    public bool PresentationRequestsPmSplitMainWindowScreenPlacement => _presentationPmForwardTwoScreen;

    /// <summary>Открывать <see cref="Views.PmSplitHostWindow"/> при старте при подходящей топологии.</summary>
    public bool OpenPmSplitHostWindowOnStartup => _settings.OpenPmSplitHostWindowOnStartup;

    /// <summary>
    /// Индекс физического дисплея для <see cref="Views.MfdHostWindow"/> в порядке
    /// <see cref="PresentationMonitorTopology.OrderScreensForPresentation"/>; <c>null</c> если строка не задаёт семантику хоста.
    /// </summary>
    public int? MfdHostPresentationScreenIndex =>
        _presentationParse.IsSuccess
        && PresentationLayoutAnalyzer.TryGetMfdHostPresentationScreenIndex(_presentationParse.Screens, out var idx)
            ? idx
            : null;

    /// <summary>
    /// Индекс дисплея для <see cref="Views.PfdHostWindow"/> в порядке
    /// <see cref="PresentationMonitorTopology.OrderScreensForPresentation"/>; <c>null</c> если строка не задаёт семантику хоста.
    /// </summary>
    public int? PfdHostPresentationScreenIndex =>
        _presentationParse.IsSuccess
        && PresentationLayoutAnalyzer.TryGetPfdHostPresentationScreenIndex(_presentationParse.Screens, out var pIdx)
            ? pIdx
            : null;

    /// <summary>Открывать окно-хост Mfd при старте (если есть ≥2 мониторов и пресет подходит).</summary>
    public bool OpenMfdHostWindowOnStartup => _settings.OpenMfdHostWindowOnStartup;

    /// <summary>Открывать окно-хост Pfd при старте (если мониторов достаточно и пресет тройной).</summary>
    public bool OpenPfdHostWindowOnStartup => _settings.OpenPfdHostWindowOnStartup;

    /// <summary>Окна <c>PfdHostWindow</c>/<c>MfdHostWindow</c> на своём дисплее — максимизировать (иначе размер по рабочей области).</summary>
    public bool MaximizePresentationHostWindowsOnDedicatedScreens =>
        _settings.MaximizePresentationHostWindowsOnDedicatedScreens;

    /// <summary>Окно-хост Mfd открыло полный вторичный контур — колонка Mfd в главном окне скрыта (см. <see cref="SetMfdHostWindowShellOpen"/>).</summary>
    public bool IsMfdHostWindowShellOpen => _suppressMfdColumnForMfdHostWindow;

    /// <summary>Окно-хост Pfd открыто — колонка Pfd в главном окне скрыта (см. <see cref="SetPfdHostWindowShellOpen"/>).</summary>
    public bool IsPfdHostWindowShellOpen => _suppressPfdColumnForPfdHostWindow;

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

    /// <summary>Сохранённая геометрия <see cref="Views.MfdHostWindow"/> в <c>settings.toml</c> (ADR 0017).</summary>
    internal bool TryGetSavedMfdHostWindowBounds(out Services.PresentationHostWindowPlacement.PresentationHostWindowBounds bounds)
    {
        var x = _settings.MfdHostWindowPixelX;
        var y = _settings.MfdHostWindowPixelY;
        var ww = _settings.MfdHostWindowWidth;
        var wh = _settings.MfdHostWindowHeight;
        if (x is null || y is null || ww is null || wh is null)
        {
            bounds = default;
            return false;
        }

        bounds = new Services.PresentationHostWindowPlacement.PresentationHostWindowBounds(x.Value, y.Value, ww.Value, wh.Value);
        return true;
    }

    /// <summary>Записать геометрию окна-хоста Mfd и при необходимости сбросить на диск.</summary>
    internal void PersistMfdHostWindowBounds(int pixelX, int pixelY, double width, double height)
    {
        _settings.MfdHostWindowPixelX = pixelX;
        _settings.MfdHostWindowPixelY = pixelY;
        _settings.MfdHostWindowWidth = width;
        _settings.MfdHostWindowHeight = height;
        SaveSettingsIfChanged();
    }

    /// <summary>Сохранённая геометрия <see cref="Views.PfdHostWindow"/> в <c>settings.toml</c> (ADR 0017).</summary>
    internal bool TryGetSavedPfdHostWindowBounds(out Services.PresentationHostWindowPlacement.PresentationHostWindowBounds bounds)
    {
        var x = _settings.PfdHostWindowPixelX;
        var y = _settings.PfdHostWindowPixelY;
        var ww = _settings.PfdHostWindowWidth;
        var wh = _settings.PfdHostWindowHeight;
        if (x is null || y is null || ww is null || wh is null)
        {
            bounds = default;
            return false;
        }

        bounds = new Services.PresentationHostWindowPlacement.PresentationHostWindowBounds(x.Value, y.Value, ww.Value, wh.Value);
        return true;
    }

    /// <summary>Записать геометрию окна-хоста Pfd и при необходимости сбросить на диск.</summary>
    internal void PersistPfdHostWindowBounds(int pixelX, int pixelY, double width, double height)
    {
        _settings.PfdHostWindowPixelX = pixelX;
        _settings.PfdHostWindowPixelY = pixelY;
        _settings.PfdHostWindowWidth = width;
        _settings.PfdHostWindowHeight = height;
        SaveSettingsIfChanged();
    }

    /// <summary>Сохранённая геометрия <see cref="Views.PmSplitHostWindow"/> в <c>settings.toml</c> (ADR 0017).</summary>
    internal bool TryGetSavedPmSplitHostWindowBounds(out Services.PresentationHostWindowPlacement.PresentationHostWindowBounds bounds)
    {
        var x = _settings.PmSplitHostWindowPixelX;
        var y = _settings.PmSplitHostWindowPixelY;
        var ww = _settings.PmSplitHostWindowWidth;
        var wh = _settings.PmSplitHostWindowHeight;
        if (x is null || y is null || ww is null || wh is null)
        {
            bounds = default;
            return false;
        }

        bounds = new Services.PresentationHostWindowPlacement.PresentationHostWindowBounds(x.Value, y.Value, ww.Value, wh.Value);
        return true;
    }

    /// <summary>Записать геометрию окна сплита P+M и при необходимости сбросить на диск.</summary>
    internal void PersistPmSplitHostWindowBounds(int pixelX, int pixelY, double width, double height)
    {
        _settings.PmSplitHostWindowPixelX = pixelX;
        _settings.PmSplitHostWindowPixelY = pixelY;
        _settings.PmSplitHostWindowWidth = width;
        _settings.PmSplitHostWindowHeight = height;
        SaveSettingsIfChanged();
    }

    /// <summary>CDS-снимок кабины (ADR 0036 п.2; см. <c>docs/design/cds-contract-v0.md</c>).</summary>
    public CockpitSurfaceState BuildCockpitSurfaceSnapshot() => CockpitSurfaceSnapshotBuilder.Build(this);
}
