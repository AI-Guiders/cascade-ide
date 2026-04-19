using CascadeIDE.Contracts.Experimental.Capabilities;

namespace CascadeIDE.Contracts.Experimental;

/// <summary>
/// Registry для регистрации capabilities модулей.
/// </summary>
/// <remarks>
/// Registry должен собирать capability-map для introspection. “Включено/доступно” вычисляется shell’ом с учётом overlay
/// (например UiMode TOML) и рантайм условий.
/// </remarks>
[ApiStability(ApiStability.Experimental)]
public interface ICapabilityRegistry
{
    /// <summary>Зарегистрировать service capability (контракт + реализация).</summary>
    void RegisterService(ServiceCapabilityDescriptor descriptor);
    /// <summary>Зарегистрировать command capability (discoverability + метаданные).</summary>
    void RegisterCommand(CommandCapabilityDescriptor descriptor);
    /// <summary>Зарегистрировать UI surface capability (панель/страница/вкладка).</summary>
    void RegisterUiSurface(UiSurfaceCapabilityDescriptor descriptor);

    /// <summary>Собрать capability-map для introspection.</summary>
    CapabilityMap BuildMap();
}

