using CascadeIDE.Contracts;
using CascadeIDE.Contracts.Experimental;
using CascadeIDE.Contracts.Experimental.Capabilities;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Markdown;

/// <summary>Регистрация capabilities для markdown tooling (v1).</summary>
[ApiStability(ApiStability.Experimental)]
public sealed class MarkdownCapabilitiesModule : ICascadeFeatureModule
{
    public string Id => CapabilityIds.DocsMarkdown.ModuleId;

    public void Register(ICapabilityRegistry registry)
    {
        registry.RegisterService(new ServiceCapabilityDescriptor
        {
            Id = CapabilityIds.DocsMarkdown.DiagramExpansionService,
            OwnerModuleId = CapabilityIds.DocsMarkdown.ModuleId,
            Stability = ApiStability.Experimental,
            ContractType = typeof(MarkdownDiagramExpansion),
            ImplementationType = typeof(MarkdownDiagramExpansion),
            Tags = ["markdown", "diagrams", "kroki"]
        });

        registry.RegisterCommand(new CommandCapabilityDescriptor
        {
            Id = CapabilityIds.DocsMarkdown.DumpCapabilitiesCommand,
            OwnerModuleId = CapabilityIds.DocsMarkdown.ModuleId,
            Stability = ApiStability.Experimental,
            Title = "Dump capabilities map to file",
            Category = "Debug/Diagnostics",
            Tags = ["debug", "diagnostics"]
        });

        registry.RegisterCommand(new CommandCapabilityDescriptor
        {
            Id = CapabilityIds.DocsMarkdown.ExportExpandedMarkdownCommand,
            OwnerModuleId = CapabilityIds.DocsMarkdown.ModuleId,
            Stability = ApiStability.Experimental,
            Title = "Export expanded Markdown",
            Category = "Markdown",
            Tags = ["markdown", "export"]
        });
    }
}

