using Avalonia.Threading;

namespace CascadeIDE.Services;

/// <summary>
/// Единая точка маршалинга на UI-поток (фаза 5). Реализация — обёртка над <see cref="Dispatcher.UIThread"/>.
/// </summary>
public interface IUiScheduler
{
    bool CheckAccess();

    void Post(Action action, DispatcherPriority priority = default);

    void Invoke(Action action);

    T Invoke<T>(Func<T> action);

    Task InvokeAsync(Action action, DispatcherPriority priority = default);

    Task<T> InvokeAsync<T>(Func<T> action, DispatcherPriority priority = default);

    Task InvokeAsync(Func<Task> action, DispatcherPriority priority = default);
}
