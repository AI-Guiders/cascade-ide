#nullable enable

namespace CascadeIDE.Cockpit.Channels;

/// <summary>
/// Generic channel coordinator contract: aggregates multiple channel outputs.
/// </summary>
public interface IChannelCoordinator<TContext, TPayload>
{
    TPayload Build(in TContext context);
}
