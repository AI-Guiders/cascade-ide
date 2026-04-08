using CascadeIDE.Contracts;
using CascadeIDE.Contracts.Experimental;
using CascadeIDE.Contracts.Experimental.Capabilities;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Markdown;

/// <summary>Регистрация capabilities для markdown tooling (v1).</summary>
[ApiStability(ApiStability.Experimental)]
public sealed class MarkdownCapabilitiesModule : ICascadeFeatureModule
{
    public const string ModuleId = "docs.markdown";

    public string Id => ModuleId;

    public void Register(ICapabilityRegistry registry)
    {
        registry.RegisterService(new ServiceCapabilityDescriptor
        {
            Id = "docs.markdown.diagram_expansion",
            OwnerModuleId = ModuleId,
            Stability = ApiStability.Experimental,
            ContractType = typeof(MarkdownDiagramExpansion),
            ImplementationType = typeof(MarkdownDiagramExpansion),
            Tags = ["markdown", "diagrams", "kroki"]
        });

        registry.RegisterCommand(new CommandCapabilityDescriptor
        {
            Id = "docs.markdown.dump_capabilities",
            OwnerModuleId = ModuleId,
            Stability = ApiStability.Experimental,
            Title = "Dump capabilities map to file",
            Category = "Debug/Diagnostics",
            Tags = ["debug", "diagnostics"]
        });
    }
}

