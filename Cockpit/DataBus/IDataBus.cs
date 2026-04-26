namespace CascadeIDE.Cockpit.DataBus;

/// <summary>
/// Typed in-process event bus contract for IDE domain signals.
/// </summary>
public interface IDataBus
{
    void Publish<TEvent>(TEvent evt);

    IDisposable Subscribe<TEvent>(Action<TEvent> handler);
}
