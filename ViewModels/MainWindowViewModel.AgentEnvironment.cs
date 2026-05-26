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
    private IDisposable? _agentEnvironmentChatProjection;
    private IDisposable? _agentEnvironmentChatProgressProjection;
    private IDisposable? _agentEnvironmentWarmupBridge;

    public Features.Agent.Environment.IAgentEnvironmentService AgentEnvironment => _agentEnvironment;

    internal void EnsureAgentEnvironmentWiring()
    {
        if (_agentEnvironmentBusSubscription is not null)
            return;

        var accounting = _settings.Agent.Environment.TimeAccounting;

        _agentEnvironmentBusSubscription = new AgentEnvironmentBusComposite(
            _ideDataBus.Subscribe<AgentEnvironmentTaskChanged>(_ =>
                UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background)),
            _ideDataBus.Subscribe<AgentRunCompleted>(_ =>
                UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background)),
            _ideDataBus.Subscribe<AgentVerifyEpochStale>(_ =>
                UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background)));

        if (accounting.ShowInChat)
        {
            _agentEnvironmentChatProjection = new AgentEnvironmentChatProjection(
                _ideDataBus,
                accounting,
                AppendAgentEnvironmentChatTrace);
        }

        _agentEnvironmentChatProgressProjection = new AgentEnvironmentChatProgressProjection(
            _ideDataBus,
            accounting,
            AppendAgentEnvironmentChatTrace);

        _agentEnvironmentWarmupBridge = new AgentEnvironmentWarmupBridge(_ideDataBus, _agentEnvironment);
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
