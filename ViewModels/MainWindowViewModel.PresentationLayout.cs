using CascadeIDE.Services.Presentation;

namespace CascadeIDE.ViewModels;

/// <summary>ADR 0017: строка <c>presentation</c> и второй <c>TopLevel</c> — <see cref="Views.MfdHostWindow"/> с полным вторичным контуром (п. 8).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Успешный разбор <see cref="CascadeIdeSettings.GetEffectivePresentationLine"/> (может быть пустой список экранов).</summary>
    public PresentationParseResult PresentationParse => _presentationParse;

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

    /// <summary>
    /// Окно-хост зоны Mfd показывает <c>SecondaryShellView</c> — скрываем колонку Mfd в главном окне, чтобы не дублировать контур.
    /// </summary>
    public void SetMfdHostWindowShellOpen(bool isOpen)
    {
        if (_suppressMfdColumnForMfdHostWindow == isOpen)
            return;

        _suppressMfdColumnForMfdHostWindow = isOpen;
        OnPropertyChanged(nameof(IsMfdColumnVisible));
        OnPropertyChanged(nameof(ActiveAttentionLayoutSurface));
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
}
