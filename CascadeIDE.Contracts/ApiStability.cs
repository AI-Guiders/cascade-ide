namespace CascadeIDE.Contracts;

/// <summary>
/// Уровень стабильности контракта SDK.
/// </summary>
/// <remarks>
/// Это внутренняя маркировка для ясности границ в active-dev (см. ADR 0024), не публичное обещание совместимости.
/// </remarks>
public enum ApiStability
{
    /// <summary>Контракт можно ломать свободно в ходе активной разработки.</summary>
    Experimental = 0,
    /// <summary>Контракт является опорным: breaking-change делается осознанно (с миграцией/заметкой).</summary>
    Stable = 1
}

/// <summary>
/// Маркер стабильности API/контракта.
/// </summary>
/// <remarks>
/// В сочетании с namespace (`...Contracts.Experimental` / `...Contracts.Stable`) даёт “видимость по умолчанию”
/// и точечную метку для анализа/инструментов.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum
                | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event,
    Inherited = false,
    AllowMultiple = false)]
public sealed class ApiStabilityAttribute : Attribute
{
    /// <summary>Создать маркер стабильности.</summary>
    public ApiStabilityAttribute(ApiStability stability) => Stability = stability;

    /// <summary>Заданная стабильность API/контракта.</summary>
    public ApiStability Stability { get; }
}

