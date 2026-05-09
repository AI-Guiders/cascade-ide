using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Узкий контракт главного окна для <see cref="MainWindowHostSurfaceProjection"/> (shell/host кадр без ссылки на конкретный VM).
/// ADR 0036 п.3, 0047; граница Cockpit / UiChrome (ширины MFD передаются отдельными параметрами проекции).
/// </summary>
public interface IMainWindowHostSurfaceInput
{
    PresentationParseResult PresentationParse { get; }

    bool IsPfdRegionExpanded { get; }

    bool IsMfdRegionExpanded { get; }

    bool IsPfdHostWindowShellOpen { get; }

    bool IsMfdHostWindowShellOpen { get; }

    DisplaySettings DisplaySettings { get; }

    string SafetyLevel { get; }
}
