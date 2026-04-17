namespace CascadeIDE.Cockpit.Channels.WorkspaceHealth;

/// <summary>
/// Один сегмент полосы build/tests/debug/git после <see cref="WorkspaceHealthSurfaceCompositor"/> (композитор, ADR 0036 п.3): текст для полосы/кокпита и флаги для шаблона поверхности.
/// </summary>
public sealed class WorkspaceHealthSegment
{
    public required WorkspaceHealthSource Source { get; init; }

    /// <summary>Полная строка (Balanced / Focus полоса).</summary>
    public required string LineText { get; init; }

    /// <summary>Короткая строка (ряд Power cockpit).</summary>
    public required string CockpitShort { get; init; }

    /// <summary>Только для <see cref="WorkspaceHealthSource.Build"/> — индикатор «идёт сборка».</summary>
    public bool IsBuildRunning { get; init; }

    public bool IsBuildSource => Source == WorkspaceHealthSource.Build;
}
