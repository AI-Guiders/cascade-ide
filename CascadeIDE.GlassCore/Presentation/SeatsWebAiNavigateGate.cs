#nullable enable

namespace CascadeIDE.GlassCore.Presentation;

/// <summary>
/// Sticky <c>web_ai_url</c> may survive non-browser PlaceOrgan; navigate WebAi only when Face targets the portal.
/// </summary>
public static class SeatsWebAiNavigateGate
{
    public static bool WantsNavigate(
        bool showFace,
        string? webAiUrl,
        string? mfdPage,
        string? faceOrgan,
        string? mOrgan,
        string? faceSeat)
    {
        if (!showFace || string.IsNullOrWhiteSpace(webAiUrl))
            return false;

        if (string.Equals(mfdPage, "WebAiPortal", StringComparison.OrdinalIgnoreCase))
            return true;

        if (IsBrowserOrgan(faceOrgan))
            return true;

        return IsBrowserOrgan(mOrgan)
               && string.Equals(faceSeat, "m", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsBrowserOrgan(string? pin) =>
        pin is not null
        && (pin.Equals("browser", StringComparison.OrdinalIgnoreCase)
            || pin.Equals("internet_browser", StringComparison.OrdinalIgnoreCase)
            || pin.Equals("scene_internet_browser", StringComparison.OrdinalIgnoreCase)
            || pin.Equals("WebAiPortal", StringComparison.OrdinalIgnoreCase)
            || pin.Equals("webai", StringComparison.OrdinalIgnoreCase)
            || pin.Equals("web_ai", StringComparison.OrdinalIgnoreCase));
}
