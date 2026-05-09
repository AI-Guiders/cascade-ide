#nullable enable

using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.EnvironmentReadiness;
using CascadeIDE.Cockpit.Composition.EnvironmentReadiness;
using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.EnvironmentReadiness.Application;

/// <summary>
/// Сценарий обновления страницы «готовность окружения» (ADR 0023/0102): канал → снимок, затем маршалинг <see cref="IEnvironmentReadinessSurfaceCompositor.Compose"/> на UI.
/// </summary>
[ApplicationOrchestrator]
public static class EnvironmentReadinessRefreshOrchestrator
{
    public static async Task RunAsync(
        IEnvironmentReadinessChannel channel,
        IEnvironmentReadinessSurfaceCompositor compositor,
        ObservableCollection<AnnunciatorLampItem> items,
        EnvironmentReadinessChannelContext context)
    {
        var rows = await channel.Build(context).ConfigureAwait(false);
        await UiScheduler.Default.InvokeAsync(() =>
        {
            compositor.Compose(
                items,
                rows,
                new EnvironmentReadinessSurfaceDecision(Enabled: true));
        });
    }
}
