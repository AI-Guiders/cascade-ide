namespace CascadeIDE.Contracts.Experimental.Capabilities;

/// <summary>
/// Общие поля capability-дескриптора.
/// </summary>
/// <remarks>
/// Дескрипторы являются частью capability-map (introspection) и должны быть стабильными по семантике полей:
/// их читают не только UI, но и диагностические/автоматизационные сценарии.
/// </remarks>
[ApiStability(ApiStability.Experimental)]
public abstract record CapabilityDescriptorBase
{
    /// <summary>Строгий идентификатор capability (используется overlay’ями и ссылками по всему продукту).</summary>
    public required string Id { get; init; }
    /// <summary>Идентификатор модуля-владельца (см. <see cref="Experimental.ICascadeFeatureModule.Id"/>).</summary>
    public required string OwnerModuleId { get; init; }
    /// <summary>Внутренняя метка стабильности контракта/описания.</summary>
    public ApiStability Stability { get; init; } = ApiStability.Experimental;
    /// <summary>Набор коротких тегов для фильтрации/группировки.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    /// <summary>Явные зависимости (ids), нужные для доступности capability.</summary>
    public IReadOnlyList<string> Requires { get; init; } = Array.Empty<string>();
}

/// <summary>Service capability: “предоставляю сервис”.</summary>
[ApiStability(ApiStability.Experimental)]
public sealed record ServiceCapabilityDescriptor : CapabilityDescriptorBase
{
    /// <summary>Тип контракта (обычно интерфейс), который предоставляет capability.</summary>
    public required Type ContractType { get; init; }
    /// <summary>Тип реализации контракта.</summary>
    public required Type ImplementationType { get; init; }
}

/// <summary>Command capability: “предоставляю команду”.</summary>
[ApiStability(ApiStability.Experimental)]
public sealed record CommandCapabilityDescriptor : CapabilityDescriptorBase
{
    /// <summary>Отображаемое имя команды.</summary>
    public string Title { get; init; } = "";
    /// <summary>Категория/группа для палитры/меню.</summary>
    public string Category { get; init; } = "";
    /// <summary>Рекомендованный hotkey (если задан).</summary>
    public string? DefaultHotkey { get; init; }
    /// <summary>
    /// Первичная зона внимания в модели кокпита (канонический id), если команда осмысленно с ней связана; иначе <see langword="null"/>.
    /// Не задаёт презентацию (диалог vs панель). ADR 0025.
    /// </summary>
    /// <seealso cref="Experimental.AttentionZoneCanonicalIds"/>
    public string? PrimaryAttentionZoneId { get; init; }
}

/// <summary>UI surface capability: “предоставляю поверхность UI”.</summary>
[ApiStability(ApiStability.Experimental)]
public sealed record UiSurfaceCapabilityDescriptor : CapabilityDescriptorBase
{
    /// <summary>Отображаемое имя поверхности UI.</summary>
    public string DisplayName { get; init; } = "";
    /// <summary>
    /// Первичная зона внимания в модели кокпита (канонический id). ADR 0025. Презентация (overlay, пиксели) — отдельно.
    /// </summary>
    /// <seealso cref="Experimental.AttentionZoneCanonicalIds"/>
    public string? PrimaryAttentionZoneId { get; init; }
    /// <summary>
    /// Стабильный id панели shell (как в <c>attention_routing</c> / <see cref="Experimental.AttentionPanelCanonicalIds"/>), если поверхность привязана к конкретной панели.
    /// Вместе с <see cref="PrimaryAttentionZoneId"/> позволяет проверить согласованность с <c>AttentionZonePanelRuntime</c>. ADR 0025.
    /// </summary>
    /// <seealso cref="Experimental.AttentionPanelCanonicalIds"/>
    public string? HostAttentionPanelId { get; init; }
}

