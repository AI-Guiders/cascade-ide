namespace CascadeIDE.Contracts.Experimental;

/// <summary>
/// Канонические строковые id зон внимания кокпита (нижний регистр). ADR 0025; совпадают с
/// <c>AttentionZoneIds</c> / TOML overlay в приложении.
/// </summary>
[ApiStability(ApiStability.Experimental)]
public static class AttentionZoneCanonicalIds
{
    public const string Forward = "forward";
    public const string Pfd = "pfd";
    public const string Mfd = "mfd";
    public const string Eicas = "eicas";
    public const string Hud = "hud";

    /// <summary>Стабильный порядок для валидации и дампов.</summary>
    public static readonly string[] All = [Forward, Pfd, Mfd, Eicas, Hud];

    /// <summary>Строгое совпадение с одним из канонических id (как в данных ADR / overlay).</summary>
    public static bool IsKnownCanonicalId(string? value) =>
        value is Forward or Pfd or Mfd or Eicas or Hud;
}
