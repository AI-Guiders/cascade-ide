using System.Threading.Channels;

namespace CascadeIDE.Cockpit.DataBus;

/// <summary>In-process implementation of <see cref="IDataBus"/>.</summary>
public sealed class InMemoryDataBus : IDataBus, IDisposable
{
    private readonly Lock _sync = new();
    private readonly Dictionary<Type, List<Delegate>> _handlers = [];
    private readonly Dictionary<Type, object> _routes = [];
    private readonly bool _asynchronousDispatch;
    private readonly CancellationTokenSource? _dispatchCts;
    private readonly DataBusEventPolicy _eventPolicy;

    public InMemoryDataBus(bool asynchronousDispatch = false, DataBusEventPolicy? eventPolicy = null)
    {
        _asynchronousDispatch = asynchronousDispatch;
        _dispatchCts = asynchronousDispatch ? new CancellationTokenSource() : null;
        _eventPolicy = eventPolicy ?? DataBusEventPolicyLoader.Load();
    }

    public void Publish<TEvent>(TEvent evt)
    {
        if (!_asynchronousDispatch)
        {
            DispatchToSubscribers(evt);
            return;
        }

        var route = GetOrCreateRoute<TEvent>();
        route.Publish(evt);
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_sync)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list))
            {
                list = [];
                _handlers[typeof(TEvent)] = list;
            }
            list.Add(handler);
        }

        return new Subscription(() => Unsubscribe(handler));
    }

    public void Dispose()
    {
        _dispatchCts?.Cancel();
        _dispatchCts?.Dispose();
    }

    private void DispatchToSubscribers<TEvent>(TEvent evt)
    {
        Delegate[] snapshot;
        lock (_sync)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list) || list.Count == 0)
                return;
            snapshot = [.. list];
        }

        foreach (var del in snapshot)
        {
            if (del is Action<TEvent> handler)
            {
                try
                {
                    handler(evt);
                }
                catch
                {
                    // Isolate subscribers: one faulty handler must not break delivery to others.
                }
            }
        }
    }

    private EventRoute<TEvent> GetOrCreateRoute<TEvent>()
    {
        lock (_sync)
        {
            if (_routes.TryGetValue(typeof(TEvent), out var existing) && existing is EventRoute<TEvent> route)
                return route;

            var created = EventRoute<TEvent>.Create(this, _eventPolicy.IsBurst(typeof(TEvent)), _dispatchCts?.Token ?? CancellationToken.None);
            _routes[typeof(TEvent)] = created;
            return created;
        }
    }

    private sealed class EventRoute<TEvent>
    {
        private readonly InMemoryDataBus _owner;
        private readonly Channel<TEvent> _channel;
        private readonly CancellationToken _cancellationToken;

        private EventRoute(InMemoryDataBus owner, Channel<TEvent> channel, CancellationToken cancellationToken)
        {
            _owner = owner;
            _channel = channel;
            _cancellationToken = cancellationToken;
            _ = Task.Run(DispatchLoopAsync, CancellationToken.None);
        }

        public static EventRoute<TEvent> Create(InMemoryDataBus owner, bool latestWinsBurst, CancellationToken cancellationToken)
        {
            if (latestWinsBurst)
            {
                var bounded = Channel.CreateBounded<TEvent>(new BoundedChannelOptions(1)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });
                return new EventRoute<TEvent>(owner, bounded, cancellationToken);
            }

            var unbounded = Channel.CreateUnbounded<TEvent>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
            return new EventRoute<TEvent>(owner, unbounded, cancellationToken);
        }

        public void Publish(TEvent evt)
        {
            _channel.Writer.TryWrite(evt);
        }

        private async Task DispatchLoopAsync()
        {
            try
            {
                await foreach (var evt in _channel.Reader.ReadAllAsync(_cancellationToken))
                    _owner.DispatchToSubscribers(evt);
            }
            catch (OperationCanceledException)
            {
                // Expected on dispose.
            }
        }
    }

    private void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        lock (_sync)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var list))
                return;
            list.Remove(handler);
            if (list.Count == 0)
                _handlers.Remove(typeof(TEvent));
        }
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose()
        {
            var d = Interlocked.Exchange(ref _dispose, null);
            d?.Invoke();
        }
    }
}
