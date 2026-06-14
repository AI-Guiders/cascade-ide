#nullable enable

using CascadeIDE.Contracts.Experimental;
using CascadeIDE.Features.Forge.Mcp;

namespace CascadeIDE.Features.Forge;

/// <summary>Forge vertical module — single registration site (ADR 0161 F4).</summary>
public sealed class ForgeFeatureModule : ICascadeFeatureModule
{
    public static ForgeFeatureModule Instance { get; } = new();

    public string Id => "forge";

    /// <inheritdoc />
    public void Register(ICapabilityRegistry registry)
    {
        registry.RegisterMcpHandlers(registrar =>
        {
            if (_pendingMcpHost is null)
                throw new InvalidOperationException("Forge MCP host context must be bound before Register.");
            ForgeMcpHandlers.Register(_pendingMcpHost, registrar);
        });
    }

    private ForgeMcpHostContext? _pendingMcpHost;

    /// <summary>Binds executor context; call before iterating <see cref="CascadeFeatureModules.All"/>.</summary>
    internal void BindMcpHost(ForgeMcpHostContext context) => _pendingMcpHost = context;

    /// <summary>Direct MCP registration used by spine executor (same handlers as <see cref="Register"/>).</summary>
    internal void RegisterMcpHandlers(ForgeMcpHostContext context, IMcpHandlerRegistrar registrar) =>
        ForgeMcpHandlers.Register(context, registrar);
}
