using CascadeIDE.Contracts.Experimental.Capabilities;

namespace CascadeIDE.Contracts.Experimental;

/// <summary>Registry для регистрации capabilities модулей.</summary>
[ApiStability(ApiStability.Experimental)]
public interface ICapabilityRegistry
{
    void RegisterService(ServiceCapabilityDescriptor descriptor);
    void RegisterCommand(CommandCapabilityDescriptor descriptor);
    void RegisterUiSurface(UiSurfaceCapabilityDescriptor descriptor);

    /// <summary>Собрать capability-map для introspection.</summary>
    CapabilityMap BuildMap();
}

