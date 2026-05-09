using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>Персистенция геометрии окон-хостов пресета <c>presentation</c> (ADR 0017).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Сохранённая геометрия <see cref="Views.MfdHostWindow"/> в <c>settings.toml</c> (ADR 0017).</summary>
    internal bool TryGetSavedMfdHostWindowBounds(
        out PresentationHostWindowPlacement.PresentationHostWindowBounds bounds)
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

        bounds =
            new PresentationHostWindowPlacement.PresentationHostWindowBounds(x.Value, y.Value, ww.Value, wh.Value);
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
    internal bool TryGetSavedPfdHostWindowBounds(
        out PresentationHostWindowPlacement.PresentationHostWindowBounds bounds)
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

        bounds =
            new PresentationHostWindowPlacement.PresentationHostWindowBounds(x.Value, y.Value, ww.Value, wh.Value);
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
    internal bool TryGetSavedPmSplitHostWindowBounds(
        out PresentationHostWindowPlacement.PresentationHostWindowBounds bounds)
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

        bounds =
            new PresentationHostWindowPlacement.PresentationHostWindowBounds(x.Value, y.Value, ww.Value, wh.Value);
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
}
