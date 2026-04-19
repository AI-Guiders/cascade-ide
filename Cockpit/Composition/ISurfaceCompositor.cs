#nullable enable

namespace CascadeIDE.Cockpit.Composition;

/// <summary>
/// Generic surface compositor contract (ADR 0036 p.3).
/// </summary>
public interface ISurfaceCompositor<TScene, TPayload, TDecision, TResult>
{
    TResult Compose(TScene scene, TPayload payload, in TDecision decision);
}
