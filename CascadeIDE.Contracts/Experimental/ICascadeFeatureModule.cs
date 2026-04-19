namespace CascadeIDE.Contracts.Experimental;

/// <summary>
/// Точка входа фичи для регистрации capabilities.
/// </summary>
/// <remarks>
/// Реестр модулей формируется code-first (явным списком), без MEF/сканирования сборок.
/// </remarks>
[ApiStability(ApiStability.Experimental)]
public interface ICascadeFeatureModule
{
    /// <summary>
    /// Стабильный идентификатор модуля.
    /// </summary>
    /// <remarks>Используется в introspection, capability-map и диагностике.</remarks>
    string Id { get; }

    /// <summary>
    /// Зарегистрировать capabilities модуля в общий registry.
    /// </summary>
    void Register(ICapabilityRegistry registry);
}

