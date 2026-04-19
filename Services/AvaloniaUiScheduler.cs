using Avalonia.Threading;

namespace CascadeIDE.Services;

/// <summary>Реализация <see cref="IUiScheduler"/> через Avalonia <see cref="Dispatcher.UIThread"/>.</summary>
public sealed class AvaloniaUiScheduler : IUiScheduler
{
    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

    public void Post(Action action, DispatcherPriority priority = default) =>
        Dispatcher.UIThread.Post(action, priority);

    public void Invoke(Action action) =>
        Dispatcher.UIThread.Invoke(action);

    public T Invoke<T>(Func<T> action) =>
        Dispatcher.UIThread.Invoke(action);

    public Task InvokeAsync(Action action, DispatcherPriority priority = default) =>
        Dispatcher.UIThread.InvokeAsync(action, priority).GetTask();

    public Task<T> InvokeAsync<T>(Func<T> action, DispatcherPriority priority = default) =>
        Dispatcher.UIThread.InvokeAsync(action, priority).GetTask();

    public Task InvokeAsync(Func<Task> action, DispatcherPriority priority = default) =>
        Dispatcher.UIThread.InvokeAsync(action, priority);

    public Task<T> InvokeAsync<T>(Func<Task<T>> action, DispatcherPriority priority = default) =>
        Dispatcher.UIThread.InvokeAsync(action, priority);
}
