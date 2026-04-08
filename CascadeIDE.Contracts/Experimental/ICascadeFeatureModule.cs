namespace CascadeIDE.Contracts.Experimental;

/// <summary>Точка входа фичи для регистрации capabilities.</summary>
[ApiStability(ApiStability.Experimental)]
public interface ICascadeFeatureModule
{
    /// <summary>Стабильный идентификатор модуля (используется в introspection).</summary>
    string Id { get; }

    /// <summary>Зарегистрировать capabilities модуля в общий registry.</summary>
    void Register(ICapabilityRegistry registry);
}

