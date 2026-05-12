#nullable enable
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Фабрика бэкенда go-to (<c>t:/m:/x:</c>) по <see cref="CommandPaletteGoToSearchSettings.Backend"/>.</summary>
internal static class CommandPaletteGoToSearchBackendFactory
{
    private static readonly RipgrepCommandPaletteGoToSearchBackend RipgrepSingleton = new();

    /// <summary>При <see cref="HybridIndexSettings.Enabled"/> = false — любой режим, кроме <c>rg</c>, эквивалентен ripgrep.</summary>
    internal static ICommandPaletteGoToSearchBackend Resolve(
        CommandPaletteGoToSearchBackendKind kind,
        HybridIndexOrchestrator hybridIndexOrchestrator,
        string hybridScopeMode,
        bool hybridIntegrationEnabled)
    {
        if (!hybridIntegrationEnabled)
            return RipgrepSingleton;

        return kind switch
        {
            CommandPaletteGoToSearchBackendKind.Hci => new HybridIndexCommandPaletteGoToSearchBackend(
                hybridIndexOrchestrator,
                hybridScopeMode),
            CommandPaletteGoToSearchBackendKind.Auto =>
                new CompositeAutoCommandPaletteGoToSearchBackend(
                    hybridIndexOrchestrator,
                    hybridScopeMode,
                    RipgrepSingleton,
                    new HybridIndexCommandPaletteGoToSearchBackend(hybridIndexOrchestrator, hybridScopeMode)),
            _ => RipgrepSingleton,
        };
    }
}
