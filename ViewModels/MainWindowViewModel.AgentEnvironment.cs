#nullable enable

using Avalonia.Threading;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Features.Chat;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>AEE: PFD status strip, chat trace, epoch stale on write (ADR 0148 W3–W5).</summary>
public partial class MainWindowViewModel
{
    private IDisposable? _agentEnvironmentBusSubscription;

    public Features.Agent.Environment.IAgentEnvironmentService AgentEnvironment => _agentEnvironment;

    internal void EnsureAgentEnvironmentWiring()
    {
        if (_agentEnvironmentBusSubscription is not null)
            return;

        _agentEnvironmentBusSubscription = new AgentEnvironmentBusComposite(
            _ideDataBus.Subscribe<AgentEnvironmentTaskChanged>(_ =>
                UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background)),
            _ideDataBus.Subscribe<AgentRunCompleted>(_ =>
                UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background)),
            _ideDataBus.Subscribe<AgentVerifyEpochStale>(_ =>
                UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background)));

        if (_settings.Agent.Environment.TimeAccounting.ShowInChat)
        {
            _ = new AgentEnvironmentChatProjection(
                _ideDataBus,
                _settings.Agent.Environment.TimeAccounting,
                AppendAgentEnvironmentChatTrace);
        }
    }

    internal void NotifyAgentEnvironmentDocumentWrite(string? filePath)
    {
        _agentEnvironment.EpochTracker.NotifyWrite(filePath);
    }

    private void AppendAgentEnvironmentChatTrace(string text, bool green)
    {
        UiScheduler.Default.Post(() =>
        {
            ChatPanel.AppendAgentEnvironmentTrace(
                text,
                green ? ChatSlashCommandStatus.Succeeded : ChatSlashCommandStatus.Failed);
        }, DispatcherPriority.Background);
    }
}

file sealed class AgentEnvironmentBusComposite : IDisposable
{
    private readonly IDisposable[] _items;

    public AgentEnvironmentBusComposite(params IDisposable[] items) => _items = items;

    public void Dispose()
    {
        foreach (var d in _items)
            d.Dispose();
    }
}
