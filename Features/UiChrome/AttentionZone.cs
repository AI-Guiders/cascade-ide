using System.Collections.Immutable;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Канонические строковые id для TOML и API (нижний регистр, латиница). ADR 0021:
/// три пространственных якоря (<see cref="Frontal"/>, <see cref="Pfd"/>, <see cref="Mfd"/>),
/// <see cref="Eicas"/> — <strong>канал</strong> оповещений (не четвёртый якорь-колонка), <see cref="Hud"/> — слой на лобовом.
/// </summary>
public static class AttentionZoneIds
{
    public const string Frontal = "frontal";
    public const string Pfd = "pfd";
    public const string Mfd = "mfd";
    public const string Eicas = "eicas";
    public const string Hud = "hud";

    /// <summary>Все допустимые id в стабильном порядке (для валидации и тестов).</summary>
    public static ImmutableArray<string> All { get; } =
        ImmutableArray.Create(Frontal, Pfd, Mfd, Eicas, Hud);
}

/// <summary>
/// Роль в модели внимания ADR 0021 (соответствует <see cref="AttentionZoneIds"/>).
/// «Зона» в продуктовом смысле — про три якоря; <see cref="Eicas"/> — канал, не зона-якорь.
/// </summary>
public enum AttentionZone
{
    /// <summary>Лобовое: редактор.</summary>
    Frontal,

    /// <summary>PFD: контекст workspace.</summary>
    Pfd,

    /// <summary>MFD: вторичные инструменты.</summary>
    Mfd,

    /// <summary>EICAS/CAS: канал оповещений W/C/A — не четвёртый якорь рядом с PFD/MFD (ADR §5).</summary>
    Eicas,

    /// <summary>HUD: слой внутри лобового.</summary>
    Hud,
}

/// <summary>Разбор и классификация <see cref="AttentionZone"/>.</summary>
public static class AttentionZoneExtensions
{
    /// <summary>Сериализация в канонический id (строго совпадает с <see cref="AttentionZoneIds"/>).</summary>
    public static string ToCanonicalId(this AttentionZone zone) =>
        zone switch
        {
            AttentionZone.Frontal => AttentionZoneIds.Frontal,
            AttentionZone.Pfd => AttentionZoneIds.Pfd,
            AttentionZone.Mfd => AttentionZoneIds.Mfd,
            AttentionZone.Eicas => AttentionZoneIds.Eicas,
            AttentionZone.Hud => AttentionZoneIds.Hud,
            _ => throw new ArgumentOutOfRangeException(nameof(zone), zone, null),
        };

    /// <summary>
    /// Разбор канонической строки. Совпадение по <see cref="StringComparison.Ordinal"/> (как в данных ADR).
    /// </summary>
    public static bool TryParseCanonicalId(string? value, out AttentionZone zone)
    {
        zone = default;
        if (string.IsNullOrEmpty(value))
            return false;

        if (value == AttentionZoneIds.Frontal)
        {
            zone = AttentionZone.Frontal;
            return true;
        }

        if (value == AttentionZoneIds.Pfd)
        {
            zone = AttentionZone.Pfd;
            return true;
        }

        if (value == AttentionZoneIds.Mfd)
        {
            zone = AttentionZone.Mfd;
            return true;
        }

        if (value == AttentionZoneIds.Eicas)
        {
            zone = AttentionZone.Eicas;
            return true;
        }

        if (value == AttentionZoneIds.Hud)
        {
            zone = AttentionZone.Hud;
            return true;
        }

        return false;
    }

    /// <summary>Три пространственных якоря: лобовое, PFD, MFD.</summary>
    public static bool IsSpatialAnchor(this AttentionZone zone) =>
        zone is AttentionZone.Frontal or AttentionZone.Pfd or AttentionZone.Mfd;

    /// <summary>Канал оповещений EICAS/CAS.</summary>
    public static bool IsAlertingChannel(this AttentionZone zone) => zone == AttentionZone.Eicas;

    /// <summary>Слой HUD только на лобовом (редактор).</summary>
    public static bool IsHudLayer(this AttentionZone zone) => zone == AttentionZone.Hud;
}
