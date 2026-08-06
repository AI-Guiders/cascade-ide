#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>Face chrome-strip helpers — Slack/MM light meta (name first; peel legacy seat chrome).</summary>
public static class GlassIntercomFaceMeta
{
    /// <summary>Quiet display name from journal RoleLabel (legacy: "Name · kind @FROM → @TO").</summary>
    public static string QuietRole(string? roleLabel)
    {
        if (string.IsNullOrWhiteSpace(roleLabel))
            return "?";

        var s = roleLabel.Trim();
        var i = s.IndexOf(" · ", StringComparison.Ordinal);
        return i > 0 ? s[..i] : s;
    }
}
