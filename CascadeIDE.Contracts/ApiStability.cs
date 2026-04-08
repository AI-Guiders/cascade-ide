namespace CascadeIDE.Contracts;

/// <summary>Уровень стабильности контракта SDK.</summary>
public enum ApiStability
{
    Experimental = 0,
    Stable = 1
}

/// <summary>
/// Маркер стабильности API/контракта. В active-dev служит внутренней ясности (см. ADR 0024),
/// не является публичным обещанием совместимости.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum
                | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event,
    Inherited = false,
    AllowMultiple = false)]
public sealed class ApiStabilityAttribute : Attribute
{
    public ApiStabilityAttribute(ApiStability stability) => Stability = stability;

    public ApiStability Stability { get; }
}

