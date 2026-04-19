namespace CascadeIDE.Services.Presentation;

/// <summary>Токены грамматики <c>presentation</c> из <c>settings.toml</c> (ADR 0017 § грамматика).</summary>
public readonly record struct PresentationGrammarTokens(
    char ScreenOpen,
    char ScreenClose,
    string ScreenSeparator,
    string ZoneSeparator,
    string PfdZoneIdentifier,
    string ForwardZoneIdentifier,
    string MfdZoneIdentifier)
{
    public static PresentationGrammarTokens Default => new('(', ')', " ", "+", "PFD", "Forward", "MFD");

    /// <summary>Разбор полей TOML; пустые строки заменяются дефолтами. При дубликатах между тремя идентификаторами якорей — сброс на дефолт.</summary>
    public static PresentationGrammarTokens FromSettings(
        string? brackets,
        string? betweenScreens,
        string? betweenZones,
        string? pfdZoneIdentifier = null,
        string? forwardZoneIdentifier = null,
        string? mfdZoneIdentifier = null)
    {
        var markers = string.IsNullOrEmpty(brackets) ? "()" : brackets.Trim();
        if (markers.Length != 2)
            markers = "()";
        var sep = string.IsNullOrEmpty(betweenScreens) ? " " : betweenScreens;
        var zone = string.IsNullOrEmpty(betweenZones) ? "+" : betweenZones;

        var def = Default;
        var pfd = NormId(pfdZoneIdentifier, def.PfdZoneIdentifier);
        var fwd = NormId(forwardZoneIdentifier, def.ForwardZoneIdentifier);
        var mfd = NormId(mfdZoneIdentifier, def.MfdZoneIdentifier);

        if (!AreZoneTokensUnique(pfd, fwd, mfd))
        {
            pfd = def.PfdZoneIdentifier;
            fwd = def.ForwardZoneIdentifier;
            mfd = def.MfdZoneIdentifier;
        }

        return new(markers[0], markers[1], sep, zone, pfd, fwd, mfd);
    }

    private static string NormId(string? value, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        var t = value.Trim();
        return t.Length > 0 ? t : defaultValue;
    }

    private static bool AreZoneTokensUnique(string pfd, string fwd, string mfd)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in new[] { pfd, fwd, mfd })
        {
            if (t.Length == 0 || !set.Add(t))
                return false;
        }

        return true;
    }
}
