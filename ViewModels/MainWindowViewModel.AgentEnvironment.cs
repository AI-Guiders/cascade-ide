#nullable enable

using Avalonia.Threading;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Features.Chat;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.ViewModels;

/// <summary>AEE: PFD Verify Epoch instrument, chat trace, epoch stale on write (ADR 0148 W3–W5).</summary>
public partial class MainWindowViewModel
{
    private readonly AgentVerifyEpochInstrument _verifyEpochInstrument = new();
    private IDisposable? _agentEnvironmentBusSubscription;
    private IDisposable? _verifyEpochDataBusBridge;
    private IDisposable? _agentEnvironmentChatProjection;
    private IDisposable? _agentEnvironmentChatProgressProjection;
    private IDisposable? _agentEnvironmentWarmupBridge;
    private IDisposable? _agentEnvironmentEpochStaleChatProjection;

    public Features.Agent.Environment.IAgentEnvironmentService AgentEnvironment => _agentEnvironment;

    public AgentVerifyEpochInstrument VerifyEpochInstrument => _verifyEpochInstrument;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PfdBackgroundStatusText))]
    [NotifyPropertyChangedFor(nameof(ShowPfdBackgroundStatusBar))]
    [NotifyPropertyChangedFor(nameof(ShowPfdVerifyEpochExpandedPanel))]
    private bool _isPfdVerifyEpochExpanded;

    public bool ShowPfdVerifyEpochExpandedPanel =>
        IsPfdVerifyEpochExpanded
        && _settings.Agent.Environment.TimeAccounting.PfdInstrumentEnabled
        && !string.IsNullOrWhiteSpace(PfdVerifyEpochExpandedText);

    public string PfdVerifyEpochExpandedText => _verifyEpochInstrument.ExpandedText;

    internal void EnsureAgentEnvironmentWiring()
    {
        if (_agentEnvironmentBusSubscription is not null)
            return;

        var accounting = _settings.Agent.Environment.TimeAccounting;

        EnsurePfdBackgroundStatusSubscription();
        EnsurePfdAgentEnvironmentTaskSubscription();

        _verifyEpochInstrument.Changed += OnVerifyEpochInstrumentChanged;

        _verifyEpochDataBusBridge = new AgentVerifyEpochDataBusBridge(_ideDataBus, _verifyEpochInstrument);

        _agentEnvironmentBusSubscription = new AgentEnvironmentBusComposite(
            _ideDataBus.Subscribe<AgentEnvironmentTaskChanged>(_ =>
                UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background)),
            _ideDataBus.Subscribe<AgentRunCompleted>(_ =>
                UiScheduler.Default.Post(RefreshPfdBackgroundStatusBar, DispatcherPriority.Background)),
            _ideDataBus.Subscribe<AgentVerifyEpochStale>(_ =>
                UiScheduler.Default.Post(OnAgentVerifyEpochStaleUi, DispatcherPriority.Background)));

        if (accounting.ShowInChat)
        {
            _agentEnvironmentChatProjection = new AgentEnvironmentChatProjection(
                _ideDataBus,
                accounting,
                AppendAgentEnvironmentChatTrace);

            _agentEnvironmentEpochStaleChatProjection = new AgentEnvironmentEpochStaleChatProjection(
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

    private void OnVerifyEpochInstrumentChanged()
    {
        UiScheduler.Default.Post(() =>
        {
            OnPropertyChanged(nameof(PfdVerifyEpochExpandedText));
            OnPropertyChanged(nameof(ShowPfdVerifyEpochExpandedPanel));
            OnPropertyChanged(nameof(ShowPfdVerifyEpochRetry));
            OnPropertyChanged(nameof(ShowPfdVerifyEpochExpandToggle));
            RefreshPfdBackgroundStatusBar();
            EnsureVerifyEpochActiveTicker();
        }, DispatcherPriority.Background);
    }

    internal void NotifyAgentEnvironmentDocumentWrite(string? filePath)
    {
        _agentEnvironment.EpochTracker.NotifyWrite(filePath);
        RefreshAgentVerifyEpochEditorDim(filePath);
    }

    private void OnAgentVerifyEpochStaleUi()
    {
        RefreshPfdBackgroundStatusBar();
        RefreshAgentVerifyEpochEditorDim(CurrentFilePath);
    }

    internal void RefreshAgentVerifyEpochEditorDim(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var isStale = _agentEnvironment.EpochTracker.IsPathUiStale(filePath);
        RefreshActiveEditorEpochDimRequested?.Invoke(isStale);
    }

    internal void NotifyCideWindowFocusChanged(bool focused) =>
        _agentEnvironment.NotifyCideWindowFocus(focused);

    /// <summary>MainWindow: применить dim к активному редактору.</summary>
    internal event Action<bool>? RefreshActiveEditorEpochDimRequested;

    [RelayCommand]
    private void TogglePfdVerifyEpochExpanded() =>
        IsPfdVerifyEpochExpanded = !IsPfdVerifyEpochExpanded;

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
