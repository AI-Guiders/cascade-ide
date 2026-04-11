using CascadeIDE.Services.Presentation;

namespace CascadeIDE.ViewModels;

/// <summary>ADR 0017: строка <c>presentation</c> и второй <c>TopLevel</c> — <see cref="Views.MfdHostWindow"/> с полным вторичным контуром (п. 8).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Успешный разбор <see cref="CascadeIdeSettings.GetEffectivePresentationLine"/> (может быть пустой список экранов).</summary>
    public PresentationParseResult PresentationParse => _presentationParse;

    /// <summary>Пресет «первый экран — PFD+Forward без MFD, второй — только MFD».</summary>
    public bool PresentationRequestsDedicatedMfdSecondScreen => _presentationDedicatedMfdSecondScreen;

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
}
