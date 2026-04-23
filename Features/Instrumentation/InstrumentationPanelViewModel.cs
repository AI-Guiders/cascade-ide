using System.Collections.ObjectModel;
using Avalonia.Threading;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Instrumentation;

/// <summary>
/// Состояние инструментирования: трасса агента, таймлайн событий, очередь задач, тесты, отладка MCP.
/// </summary>
public sealed partial class InstrumentationPanelViewModel : ViewModelBase
{
    public InstrumentationPanelViewModel()
    {
        AgentToolCalls.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAgentToolCalls));
        AgentTraceSteps.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAgentTraceSteps));
        PowerTaskQueueItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasPowerTaskQueueItems));
        EventTimeline.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasEventTimeline));
        DebugStackFrames.CollectionChanged += (_, _) => OnDebugCollectionsChanged();
        DebugVariableRoots.CollectionChanged += (_, _) => OnDebugCollectionsChanged();
    }

    private void OnDebugCollectionsChanged()
    {
        OnPropertyChanged(nameof(IsDebugPanelVisible));
    }

    public ObservableCollection<string> AgentToolCalls { get; } = [];
    public ObservableCollection<AgentTraceStepViewModel> AgentTraceSteps { get; } = [];
    public ObservableCollection<string> EventTimeline { get; } = [];
    public ObservableCollection<PowerTaskQueueItemViewModel> PowerTaskQueueItems { get; } = [];

    public bool HasAgentToolCalls => AgentToolCalls.Count > 0;
    public bool HasAgentTraceSteps => AgentTraceSteps.Count > 0;
    public bool HasPowerTaskQueueItems => PowerTaskQueueItems.Count > 0;
    public bool HasEventTimeline => EventTimeline.Count > 0;

    public ObservableCollection<DebugStackFrameViewModel> DebugStackFrames { get; } = [];
    public ObservableCollection<DebugVariableNodeViewModel> DebugVariableRoots { get; } = [];

    public bool IsDebugPanelVisible => DebugStackFrames.Count > 0 || DebugVariableRoots.Count > 0;

    /// <summary>Накопленный текстовый лог прогонов тестов для вкладки «Тесты».</summary>
    [ObservableProperty]
    private string _testResultsOutput = "";

    /// <summary>Добавить шаг в Agent Trace Timeline (Power); потокобезопасно.</summary>
    public void AppendAgentTraceStep(string kind, string text, string status, DateTimeOffset? at = null)
    {
        void Add()
        {
            AgentTraceSteps.Add(new AgentTraceStepViewModel(kind, text, status, at));
            while (AgentTraceSteps.Count > 200)
                AgentTraceSteps.RemoveAt(0);
        }

        if (UiScheduler.Default.CheckAccess())
            Add();
        else
            UiScheduler.Default.Post(Add);
    }
}
