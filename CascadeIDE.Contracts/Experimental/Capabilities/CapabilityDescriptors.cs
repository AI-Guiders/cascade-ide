namespace CascadeIDE.Contracts.Experimental.Capabilities;

/// <summary>Общие поля capability-дескриптора.</summary>
[ApiStability(ApiStability.Experimental)]
public abstract record CapabilityDescriptorBase
{
    public required string Id { get; init; }
    public required string OwnerModuleId { get; init; }
    public ApiStability Stability { get; init; } = ApiStability.Experimental;
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Requires { get; init; } = Array.Empty<string>();
}

[ApiStability(ApiStability.Experimental)]
public sealed record ServiceCapabilityDescriptor : CapabilityDescriptorBase
{
    public required Type ContractType { get; init; }
    public required Type ImplementationType { get; init; }
}

[ApiStability(ApiStability.Experimental)]
public sealed record CommandCapabilityDescriptor : CapabilityDescriptorBase
{
    public string Title { get; init; } = "";
    public string Category { get; init; } = "";
    public string? DefaultHotkey { get; init; }
}

[ApiStability(ApiStability.Experimental)]
public sealed record UiSurfaceCapabilityDescriptor : CapabilityDescriptorBase
{
    public string DisplayName { get; init; } = "";
}

