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

    /// <summary>Зарегистрировать MCP handlers vertical module (ADR 0161).</summary>
    void RegisterMcpHandlers(Action<IMcpHandlerRegistrar> register);

    /// <summary>Собрать capability-map для introspection.</summary>
    CapabilityMap BuildMap();
}

/// <summary>MCP handler registration callback (command id → async handler).</summary>
[ApiStability(ApiStability.Experimental)]
public interface IMcpHandlerRegistrar
{
    void Add(string commandId, McpCommandHandler handler);
}

/// <summary>Async MCP command handler invoked by the IDE MCP executor.</summary>
public delegate Task<string> McpCommandHandler(
    IReadOnlyDictionary<string, System.Text.Json.JsonElement>? args,
    CancellationToken cancellationToken);
