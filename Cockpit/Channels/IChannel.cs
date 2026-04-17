#nullable enable

namespace CascadeIDE.Cockpit.Channels;

/// <summary>
/// Generic channel contract (ADR 0036 p.1): maps domain context into semantic payload.
/// </summary>
public interface IChannel<TContext, TPayload>
{
    TPayload Build(in TContext context);
}
