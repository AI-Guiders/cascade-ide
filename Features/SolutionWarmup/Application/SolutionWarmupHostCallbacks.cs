#nullable enable

using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;

namespace CascadeIDE.Features.SolutionWarmup.Application;

/// <summary>Доступ оркестратора к UI/редактору без ссылки на ViewModels.</summary>
public sealed class SolutionWarmupHostCallbacks
{
    public Func<string?> GetActiveCsFilePath { get; init; } = static () => null;

    public Func<IReadOnlyList<string>> GetOpenCsFilePaths { get; init; } = static () => [];

    public Action RunFeedAnchorsOnUi { get; init; } = static () => { };

    public Func<SolutionWarmupSettings> GetWarmupSettings { get; init; } = static () => new();

    public Func<HybridIndexSettings> GetHybridIndexSettings { get; init; } = static () => new();

    public Func<HybridIndexStateChanged?> GetLatestHybridIndexState { get; init; } = static () => null;
}
