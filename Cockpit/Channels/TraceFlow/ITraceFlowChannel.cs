#nullable enable
using CascadeIDE.Cockpit.Channels;

namespace CascadeIDE.Cockpit.Channels.TraceFlow;

/// <summary>
/// Trace-flow channel contract (ADR 0036 p.1): produces semantic trace payload from domain data.
/// </summary>
public interface ITraceFlowChannel : IChannel<TraceFlowChannelContext, TraceFlowChannelSnapshot>
{
}
