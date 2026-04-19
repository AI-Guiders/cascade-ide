#nullable enable

namespace CascadeIDE.Cockpit.Cds;

/// <summary>
/// Generic CDS routing contract (ADR 0036 p.2): maps domain-specific input into CDS routing decision.
/// </summary>
public interface ICdsRouter<in TInput, out TDecision>
{
    TDecision Route(TInput input);
}
