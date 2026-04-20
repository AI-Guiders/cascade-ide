#nullable enable
using CascadeIDE.Cockpit.Channels;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Channels.EnvironmentReadiness;

/// <summary>
/// Канал снимка «готовность окружения» (ADR 0023): отдаёт строки <see cref="AnnunciatorLampItem"/>, ту же визуальную шкалу W/C/A, что и полоса EICAS, но другой источник данных.
/// </summary>
public interface IEnvironmentReadinessChannel : IChannel<EnvironmentReadinessChannelContext, ValueTask<IReadOnlyList<AnnunciatorLampItem>>>
{
}
