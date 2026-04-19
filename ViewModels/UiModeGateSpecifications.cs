using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

internal readonly record struct UiModeGateContext(
    UiModeFamily UiModeFamily,
    bool AutonomousAgentTelemetryEnabled,
    bool TerminalVisible,
    bool HasDebugSession);

internal interface IUiModeGateSpecification
{
    bool IsSatisfiedBy(in UiModeGateContext context);
}

internal sealed class UiModeFamilyGateSpecification(UiModeFamily family) : IUiModeGateSpecification
{
    public bool IsSatisfiedBy(in UiModeGateContext context) => context.UiModeFamily == family;
}

internal sealed class TelemetryEnabledGateSpecification : IUiModeGateSpecification
{
    public bool IsSatisfiedBy(in UiModeGateContext context) => context.AutonomousAgentTelemetryEnabled;
}

internal sealed class TerminalHiddenGateSpecification : IUiModeGateSpecification
{
    public bool IsSatisfiedBy(in UiModeGateContext context) => !context.TerminalVisible;
}

internal sealed class AndUiModeGateSpecification(params IUiModeGateSpecification[] specifications) : IUiModeGateSpecification
{
    public bool IsSatisfiedBy(in UiModeGateContext context)
    {
        foreach (var specification in specifications)
        {
            if (!specification.IsSatisfiedBy(in context))
                return false;
        }

        return true;
    }
}

internal static class UiModeGateSpecifications
{
    // "Show telemetry hidden hint only if Power + telemetry enabled + terminal hidden."
    public static readonly IUiModeGateSpecification ShowTelemetryHiddenHint = new AndUiModeGateSpecification(
        new UiModeFamilyGateSpecification(UiModeFamily.Power),
        new TelemetryEnabledGateSpecification(),
        new TerminalHiddenGateSpecification());
}
