using CascadeIDE.Cockpit.Cds;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>
/// Определяет активную топологию зон внимания по флагам презентации и хостов PFD/MFD (ADR 0017, 0021).
/// </summary>
public static class AttentionLayoutSurfaceResolver
{
    public static AttentionLayoutSurfaceKind Resolve(
        bool suppressPfdColumnForPfdHostWindow,
        bool suppressMfdColumnForMfdHostWindow,
        bool presentationRequestsPfdHostWindow,
        bool presentationMfdHostTopology)
    {
        if (suppressPfdColumnForPfdHostWindow
            && suppressMfdColumnForMfdHostWindow
            && presentationRequestsPfdHostWindow)
            return AttentionLayoutSurfaceKind.MainWindowPlusPfdMfdHostTopLevel;
        if (suppressPfdColumnForPfdHostWindow && presentationRequestsPfdHostWindow)
            return AttentionLayoutSurfaceKind.MainWindowPlusPfdHostTopLevel;
        if (suppressMfdColumnForMfdHostWindow && presentationMfdHostTopology)
            return AttentionLayoutSurfaceKind.MainWindowPlusMfdHostTopLevel;
        return AttentionLayoutSurfaceKind.MainWindowDockedGrid;
    }
}
