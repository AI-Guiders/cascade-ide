#nullable enable

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// Glass MFD Editor Face policy: AvalonEdit on <c>MfdEditorHost</c> when page=Editor.
/// Never Avalonia <c>FormatMfdStub</c> peel — even when Forward owns the primary work surface (ADR 0120).
/// Leave Editor page → host restores ADR 0120 home (Forward or parked on M when Intercom owns Forward).
/// </summary>
public static class GlassEditorFace
{
    public const string MfdPage = "Editor";

    public static bool IsEditorPage(string? mfdPage) =>
        string.Equals(mfdPage, MfdPage, StringComparison.OrdinalIgnoreCase);

    /// <summary>True → mount+show AvalonEdit on M; false → release Face (ADR 0120 remount).</summary>
    public static bool PreferEditorHost(string? mfdPage) => IsEditorPage(mfdPage);

    /// <summary>
    /// Where AvalonEdit lives when MFD is not showing the Editor Face.
    /// Intercom owns Forward → park on M (hidden). Else → Forward editor host.
    /// </summary>
    public static bool PreferParkOnMfdWhenReleased(bool isIntercomForward) => isIntercomForward;
}
