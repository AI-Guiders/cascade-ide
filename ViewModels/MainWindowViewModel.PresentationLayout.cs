using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.ViewModels;

/// <summary>ADR 0017: строка <c>presentation</c> и второй <c>TopLevel</c> — <see cref="Views.MfdHostWindow"/> с полным вторичным контуром (п. 8).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Свойства, зависящие от <c>_suppressMfdColumnForMfdHostWindow</c> при открытии/закрытии окна-хоста MFD.</summary>
    private static readonly string[] MfdHostShellOpenInvalidatedPropertyNames =
    [
        nameof(IsMfdHostWindowShellOpen),
        nameof(IsMfdColumnVisible),
        nameof(IsSkiaZonePreviewMfdVisible),
        nameof(MfdRegionPixelWidth),
        nameof(IsMfdRegionVisible),
        nameof(ActiveAttentionLayoutSurface),
        nameof(MainGridColumnDefinitions),
        nameof(IsPfdWorkspaceHealthMountVisible),
        nameof(IsMfdWorkspaceHealthMountVisible),
        nameof(IsMfdHostWindowWorkspaceHealthMountVisible),
        nameof(PfdWorkspaceHealthMountContext),
        nameof(MfdWorkspaceHealthMountContext),
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
            _suppressMfdColumnForMfdHostWindow);

    /// <summary>
    /// Пресет требует развернуть главное окно на весь экран при старте — см.
    /// <see cref="PresentationLayoutAnalyzer.ShouldMaximizeMainWindowAtStartup"/>.
    /// </summary>
    public bool PresentationRequestsMainWindowMaximized =>
        _presentationParse.IsSuccess
        && PresentationLayoutAnalyzer.ShouldMaximizeMainWindowAtStartup(_presentationParse.Screens);

    /// <summary>Пресет «первый экран — PFD+Forward без MFD, второй — только MFD».</summary>
    public bool PresentationRequestsDedicatedMfdSecondScreen => _presentationDedicatedMfdSecondScreen;

    /// <summary>Три дисплея: <c>(PFD) (Forward) (MFD)</c>.</summary>
    public bool PresentationRequestsTriplePfdForwardMfd => _presentationTriplePfdForwardMfd;

    /// <summary>Пресет с выносом MFD на отдельный <c>TopLevel</c> (два или три дисплея по строке <c>presentation</c>).</summary>
    public bool PresentationRequestsMfdHostWindow => _presentationMfdHostTopology;

    /// <summary>
    /// Индекс физического дисплея для <see cref="Views.MfdHostWindow"/> в порядке
    /// <see cref="PresentationMonitorTopology.OrderScreensForPresentation"/>; <c>null</c> если строка не задаёт семантику хоста.
    /// </summary>
    public int? MfdHostPresentationScreenIndex =>
        _presentationParse.IsSuccess
        && PresentationLayoutAnalyzer.TryGetMfdHostPresentationScreenIndex(_presentationParse.Screens, out var idx)
            ? idx
            : null;

    /// <summary>Открывать окно-хост Mfd при старте (если есть ≥2 мониторов и пресет подходит).</summary>
    public bool OpenMfdHostWindowOnStartup => _settings.OpenMfdHostWindowOnStartup;

    /// <summary>Окно-хост Mfd открыло полный вторичный контур — колонка Mfd в главном окне скрыта (см. <see cref="SetMfdHostWindowShellOpen"/>).</summary>
    public bool IsMfdHostWindowShellOpen => _suppressMfdColumnForMfdHostWindow;

    /// <summary>
    /// Окно-хост зоны Mfd показывает <c>SecondaryShellView</c> (чат, терминал, обозреватель решения и т.д.) — скрываем колонку Mfd в главном окне, чтобы не дублировать контур.
    /// </summary>
    public void SetMfdHostWindowShellOpen(bool isOpen)
    {
        if (_suppressMfdColumnForMfdHostWindow == isOpen)
            return;

        _suppressMfdColumnForMfdHostWindow = isOpen;
        foreach (var name in MfdHostShellOpenInvalidatedPropertyNames)
            OnPropertyChanged(name);
    }

    /// <summary>Сохранённая геометрия <see cref="Views.MfdHostWindow"/> в <c>settings.toml</c> (ADR 0017).</summary>
    internal bool TryGetSavedMfdHostWindowBounds(out Services.MfdHostWindowPlacement.MfdHostWindowBounds bounds)
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

        bounds = new Services.MfdHostWindowPlacement.MfdHostWindowBounds(x.Value, y.Value, ww.Value, wh.Value);
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

    /// <summary>CDS-снимок кабины (ADR 0036 п.2; см. <c>docs/design/cds-contract-v0.md</c>).</summary>
    public CockpitSurfaceState BuildCockpitSurfaceSnapshot() => CockpitSurfaceSnapshotBuilder.Build(this);
}
