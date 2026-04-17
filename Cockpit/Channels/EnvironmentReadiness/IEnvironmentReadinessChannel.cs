#nullable enable
using CascadeIDE.Cockpit.Channels;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Channels.EnvironmentReadiness;

/// <summary>
/// Channel contract for building Environment Readiness payload.
/// </summary>
public interface IEnvironmentReadinessChannel : IChannel<EnvironmentReadinessChannelContext, ValueTask<IReadOnlyList<EnvironmentReadinessItem>>>
{
}
