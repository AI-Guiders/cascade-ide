using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using IntercomService.Contracts;

namespace IntercomService.Services;

public sealed class SseEventHub
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<TransportEventEnvelopeDto>>> _teamChannels = new(StringComparer.Ordinal);

    public int GetSubscriberCount(string teamId) =>
        _teamChannels.TryGetValue(teamId, out var subscribers) ? subscribers.Count : 0;

    public void Publish(string teamId, TransportEventEnvelopeDto envelope)
    {
        if (!_teamChannels.TryGetValue(teamId, out var subscribers))
            return;

        foreach (var (_, channel) in subscribers)
            channel.Writer.TryWrite(envelope);
    }

    public async IAsyncEnumerable<TransportEventEnvelopeDto> SubscribeAsync(
        string teamId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<TransportEventEnvelopeDto>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        var subscribers = _teamChannels.GetOrAdd(teamId, static _ => new ConcurrentDictionary<Guid, Channel<TransportEventEnvelopeDto>>());
        subscribers[id] = channel;

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return item;
        }
        finally
        {
            subscribers.TryRemove(id, out _);
            if (subscribers.IsEmpty)
                _teamChannels.TryRemove(teamId, out _);
        }
    }
}
