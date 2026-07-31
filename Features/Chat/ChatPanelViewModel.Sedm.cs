#nullable enable
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>SEDM scope strip, session-event cache, workline resolve.</summary>
public partial class ChatPanelViewModel
{
    private readonly List<ChatHistoryEvent> _sessionEventsCache = [];

    private ChatSedmScopeStrip _sedmScopeStrip = ChatSedmScopeStrip.Empty;

    public ChatSedmScopeStrip SedmScopeStrip => _sedmScopeStrip;

    private IReadOnlyList<ChatSedmTimelineEntry> BuildSedmTimelineEntries() =>
        SedmTimelineBuilder.Build(_sessionEventsCache, ResolveSedmWorklineId());

    public string GetSedmScopeJson()
    {
        var strip = _sedmScopeStrip;
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            strip_text = strip.FormatStripText(),
            context = strip.ContextOneLiner,
            intent = strip.IntentOneLiner,
            decision = strip.DecisionOneLiner,
            decision_status = strip.DecisionStatus,
            open_worklines = strip.OpenWorklineCount,
            intent_incomplete = strip.IntentIncomplete,
        }, ChatHistoryJson.Options);
    }

    private Guid ResolveSedmWorklineId()
    {
        if (SelectedChatThreadId != Guid.Empty)
            return SelectedChatThreadId;
        if (_activeThreadId != Guid.Empty)
            return _activeThreadId;
        return _mainThreadId;
    }

    private int ResolveOpenWorklineCount()
    {
        var ids = new HashSet<Guid>();
        if (_mainThreadId != Guid.Empty)
            ids.Add(_mainThreadId);
        foreach (var msg in ChatMessages)
        {
            if (msg.ThreadId != Guid.Empty)
                ids.Add(msg.ThreadId);
        }
        foreach (var fork in _threadForks)
        {
            if (fork.NewThreadId != Guid.Empty)
                ids.Add(fork.NewThreadId);
        }
        return Math.Max(1, ids.Count);
    }

    private void RebuildSedmScopeStrip()
    {
        var worklineId = ResolveSedmWorklineId();
        var openCount = ResolveOpenWorklineCount();
        var projection = SedmEventProjector.Project(
            _sessionEventsCache,
            worklineId,
            _threadDisplayTitles,
            openCount);
        var workline = SedmEventProjector.ResolveWorkline(projection, worklineId);
        _sedmScopeStrip = ChatSedmScopeStrip.FromProjection(workline, openCount);
    }

    private void ReplaceSessionEventsCache(IReadOnlyList<ChatHistoryEvent> events)
    {
        _sessionEventsCache.Clear();
        _sessionEventsCache.AddRange(events);
        RebuildSedmScopeStrip();
    }

    private void AppendSessionEventCache(ChatHistoryEvent ev)
    {
        _sessionEventsCache.Add(ev);
        RebuildSedmScopeStrip();
    }

    public string GetSedmScopeStripText() => _sedmScopeStrip.FormatStripText();
}
