#nullable enable

using CascadeIDE.Contracts.Experimental;

namespace CascadeIDE.Features.Forge;

/// <summary>Explicit in-solution feature modules (ADR 0024 / 0161; MEF deferred per ADR 0005).</summary>
public static class CascadeFeatureModules
{
    private static readonly ICascadeFeatureModule[] Modules =
    [
        ForgeFeatureModule.Instance,
    ];

    public static IReadOnlyList<ICascadeFeatureModule> All => Modules;
}
