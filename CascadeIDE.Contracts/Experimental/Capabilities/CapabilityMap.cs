namespace CascadeIDE.Contracts.Experimental.Capabilities;

/// <summary>
/// Неизменяемое описание зарегистрированных capabilities (для introspection).
/// </summary>
/// <remarks>
/// “Включено/доступно” вычисляется shell’ом с учётом overlay (например UiMode TOML) и рантайм условий. Карта описывает
/// зарегистрированную поверхность возможностей и предназначена для диагностики и автоматизации.
/// </remarks>
[ApiStability(ApiStability.Experimental)]
public sealed record CapabilityMap
{
    /// <summary>Service capabilities.</summary>
    public IReadOnlyList<ServiceCapabilityDescriptor> Services { get; init; } = Array.Empty<ServiceCapabilityDescriptor>();
    /// <summary>Command capabilities.</summary>
    public IReadOnlyList<CommandCapabilityDescriptor> Commands { get; init; } = Array.Empty<CommandCapabilityDescriptor>();
    /// <summary>UI surface capabilities.</summary>
    public IReadOnlyList<UiSurfaceCapabilityDescriptor> UiSurfaces { get; init; } = Array.Empty<UiSurfaceCapabilityDescriptor>();

    /// <summary>Хэш содержимого карты (для “тонких снапшотов” и кэширования).</summary>
    public string? Hash { get; init; }
}

