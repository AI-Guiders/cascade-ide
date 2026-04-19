using CascadeIDE.Contracts;
using CascadeIDE.Contracts.Experimental;
using CascadeIDE.Contracts.Experimental.Capabilities;

namespace CascadeIDE.Features.UiChrome;

/// <summary>Регистрация UI surface capabilities для shell (вертикальный срез ADR 0025).</summary>
[ApiStability(ApiStability.Experimental)]
public sealed class UiChromeCapabilitiesModule : ICascadeFeatureModule
{
    public string Id => CapabilityIds.UiChrome.ModuleId;

    public void Register(ICapabilityRegistry registry)
    {
        registry.RegisterUiSurface(new UiSurfaceCapabilityDescriptor
        {
            Id = CapabilityIds.UiChrome.SolutionExplorerSurface,
            OwnerModuleId = CapabilityIds.UiChrome.ModuleId,
            Stability = ApiStability.Experimental,
            DisplayName = "Solution Explorer",
            PrimaryAttentionZoneId = AttentionZoneCanonicalIds.Pfd,
            HostAttentionPanelId = AttentionPanelCanonicalIds.SolutionExplorer,
            Tags = ["shell", "solution", "pfd"]
        });
    }
}
