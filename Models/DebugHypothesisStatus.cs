namespace CascadeIDE.Models;

/// <summary>Статус гипотезы в <c>.cascade-ide/debug-hypotheses.json</c> (ADR 0001).</summary>
public enum DebugHypothesisStatus
{
    Open,
    Rejected,
    Confirmed,
}
