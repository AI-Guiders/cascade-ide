namespace CascadeIDE.Contracts.Experimental.Capabilities;

/// <summary>Неизменяемое описание зарегистрированных capabilities (для introspection).</summary>
[ApiStability(ApiStability.Experimental)]
public sealed record CapabilityMap
{
    public IReadOnlyList<ServiceCapabilityDescriptor> Services { get; init; } = Array.Empty<ServiceCapabilityDescriptor>();
    public IReadOnlyList<CommandCapabilityDescriptor> Commands { get; init; } = Array.Empty<CommandCapabilityDescriptor>();
    public IReadOnlyList<UiSurfaceCapabilityDescriptor> UiSurfaces { get; init; } = Array.Empty<UiSurfaceCapabilityDescriptor>();

    /// <summary>Хэш содержимого карты (для “тонких снапшотов” и кэширования).</summary>
    public string? Hash { get; init; }
}

