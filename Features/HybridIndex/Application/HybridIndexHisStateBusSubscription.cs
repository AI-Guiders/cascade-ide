#nullable enable
using Avalonia.Threading;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>
/// Подписка HIS на <see cref="HybridIndexStateChanged"/> в шине IDE: маршалинг на UI-поток
/// (<see cref="DispatcherPriority.Background"/>) перед обновлением снимка состояния.
/// </summary>
[DataBusSubscriber("hybrid-index-his")]
[UiThreadMarshal("bus → IUiScheduler.Post Background")]
public static class HybridIndexHisStateBusSubscription
{
    public static IDisposable Subscribe(
        IDataBus dataBus,
        IUiScheduler ui,
        Action<HybridIndexStateChanged> apply)
        => dataBus.Subscribe<HybridIndexStateChanged>(evt =>
            ui.Post(() => apply(evt), DispatcherPriority.Background));
}
