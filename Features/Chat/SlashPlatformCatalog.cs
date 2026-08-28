#nullable enable
using AIGuiders.Platform.CommandPlane;
using CascadeIDE.Features.Forge.Infrastructure;
using CideSlashRouteEntry = CascadeIDE.Services.SlashRouteEntry;
using CidePathRole = CascadeIDE.Services.SlashPathRole;
using PlatformPathRole = AIGuiders.Platform.CommandPlane.SlashPathRole;
using PlatformSlashRouteEntry = AIGuiders.Platform.CommandPlane.SlashRouteEntry;
using PlatformArgTailKind = AIGuiders.Platform.CommandPlane.SlashArgTailKind;

namespace CascadeIDE.Features.Chat;

/// <summary>CIDE + Forge overlay → platform <see cref="SlashCatalogIndex"/> (GUIDERS-ADR-0011).</summary>
internal static class SlashPlatformCatalog
{
    private static readonly Lazy<SlashCatalogIndex> Lazy = new(Build);

    internal static SlashCatalogIndex Instance => Lazy.Value;

    static SlashCatalogIndex Build()
    {
        var entries = new List<PlatformSlashRouteEntry>();
        foreach (var route in IntentSlashCatalog.SlashRoutes.Values)
            entries.Add(ToPlatform(route));

        foreach (var route in ForgeSlashCatalogOverlay.AllRoutes)
            entries.Add(ToPlatform(route));

        return SlashCatalogIndex.FromEntries(entries);
    }

    static PlatformSlashRouteEntry ToPlatform(CideSlashRouteEntry route)
    {
        var path = IntentSlashCatalog.NormalizeSlashPath(route.SlashPath).TrimStart('/');
        var sem = route.SemanticFields;
        return new PlatformSlashRouteEntry(
            path,
            route.CommandId,
            route.Help,
            ResolveArgTailKind(route),
            sem.Domain,
            sem.Object ?? "",
            sem.Intent ?? "",
            MapPathRole(sem.PathRole),
            route.Group);
    }

    static PlatformArgTailKind ResolveArgTailKind(CideSlashRouteEntry route)
    {
        if (route.ArgTailKindExplicit is { } explicitKind)
            return MapArgTail(explicitKind);

        return MapArgTail(SlashRouteCatalogIndex.GetArgTailKind(route.SlashPath));
    }

    static PlatformPathRole MapPathRole(CidePathRole role) =>
        role == CidePathRole.Alias ? PlatformPathRole.Alias : PlatformPathRole.Canonical;

    static PlatformArgTailKind MapArgTail(CascadeIDE.Features.Chat.SlashArgTailKind kind) =>
        kind switch
        {
            CascadeIDE.Features.Chat.SlashArgTailKind.None => PlatformArgTailKind.None,
            CascadeIDE.Features.Chat.SlashArgTailKind.Required => PlatformArgTailKind.Required,
            _ => PlatformArgTailKind.Optional,
        };
}
