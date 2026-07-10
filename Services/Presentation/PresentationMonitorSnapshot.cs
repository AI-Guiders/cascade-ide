namespace CascadeIDE.Services.Presentation;

/// <summary>Physical monitors at resolve time (ADR 0171 auto tier).</summary>
public readonly record struct PresentationMonitorSnapshot(
    int PhysicalScreenCount,
    int PrimaryWorkingAreaWidthPx,
    int PrimaryWorkingAreaHeightPx,
    int TotalWorkingAreaWidthPx)
{
    public static PresentationMonitorSnapshot SingleFallback { get; } = new(1, 1920, 1080, 1920);
}
