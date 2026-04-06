namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Один сегмент полосы build/tests/debug/git: текст для классического режима и для Power cockpit, плюс флаги для шаблона.
/// </summary>
public sealed class WorkspaceTelemetrySegment
{
    public required WorkspaceTelemetrySource Source { get; init; }

    /// <summary>Полная строка (Balanced / Focus полоса).</summary>
    public required string LineText { get; init; }

    /// <summary>Короткая строка (ряд Power cockpit).</summary>
    public required string CockpitShort { get; init; }

    /// <summary>Только для <see cref="WorkspaceTelemetrySource.Build"/> — индикатор «идёт сборка».</summary>
    public bool IsBuildRunning { get; init; }

    public bool IsBuildSource => Source == WorkspaceTelemetrySource.Build;
}
