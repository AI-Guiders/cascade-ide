#nullable enable
using Avalonia.Threading;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.SolutionWarmup.Application;

[DataBusSubscriber("solution-warmup-his")]
[UiThreadMarshal("bus → IUiScheduler.Post Background")]
public static class SolutionWarmupStateBusSubscription
{
    public static IDisposable Subscribe(
        IDataBus dataBus,
        IUiScheduler ui,
        Action<SolutionWarmupStateChanged> apply) =>
        dataBus.Subscribe<SolutionWarmupStateChanged>(evt =>
            ui.Post(() => apply(evt), DispatcherPriority.Background));
}
