using CascadeIDE.Cockpit.DataBus;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Subscribes DataBus and feeds <see cref="AgentVerifyEpochInstrument"/>.</summary>
public sealed class AgentVerifyEpochDataBusBridge : IDisposable
{
    private readonly AgentVerifyEpochInstrument _instrument;
    private readonly IDisposable _composite;

    public AgentVerifyEpochDataBusBridge(IDataBus dataBus, AgentVerifyEpochInstrument instrument)
    {
        _instrument = instrument;
        _composite = new CompositeDisposable(
            dataBus.Subscribe<AgentRunStarted>(_instrument.OnRunStarted),
            dataBus.Subscribe<AgentEnvironmentTaskChanged>(_instrument.OnTaskChanged),
            dataBus.Subscribe<AgentEnvironmentTaskCompleted>(_instrument.OnTaskCompleted),
            dataBus.Subscribe<AgentEnvironmentTaskDied>(_instrument.OnTaskDied),
            dataBus.Subscribe<AgentRunCompleted>(_instrument.OnRunCompleted),
            dataBus.Subscribe<AgentVerifyEpochStale>(_instrument.OnEpochStale));
    }

    public void Dispose() => _composite.Dispose();

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IDisposable[] _items;

        public CompositeDisposable(params IDisposable[] items) => _items = items;

        public void Dispose()
        {
            foreach (var item in _items)
                item.Dispose();
        }
    }
}
