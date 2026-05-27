namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Единая точка verify после шага агента (ADR 0148 W6/E).</summary>
public interface IAgentOrchestrator
{
    AgentVerifyStartResult TryVerifyAfterStep(bool writesOccurred, bool longRun = false);
}
