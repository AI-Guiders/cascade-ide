using System.Collections.ObjectModel;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Собирает единый упорядоченный список сегментов полосы телеметрии из уже вычисленных строк
/// (<see cref="ViewModels.MainWindowViewModel"/> / <see cref="UiChromeViewModel"/>).
/// Единая точка порядка и состава (ADR 0021 / идея «одного композитора стекла»); новые источники — через
/// <see cref="WorkspaceTelemetryInputSnapshot"/>, а не отдельные параметры на каждый вызов.
/// </summary>
public static class WorkspaceTelemetryCompositor
{
    /// <summary>Порядок сегментов на полосе: сборка → тесты → отладка → git.</summary>
    public static void Rebuild(ObservableCollection<WorkspaceTelemetrySegment> target, WorkspaceTelemetryInputSnapshot inputs)
    {
        target.Clear();
        Append(target, WorkspaceTelemetrySource.Build, inputs.Build);
        Append(target, WorkspaceTelemetrySource.Tests, inputs.Tests);
        Append(target, WorkspaceTelemetrySource.Debug, inputs.Debug);
        Append(target, WorkspaceTelemetrySource.Git, inputs.Git);
    }

    private static void Append(
        ObservableCollection<WorkspaceTelemetrySegment> target,
        WorkspaceTelemetrySource source,
        WorkspaceTelemetrySegmentInput input)
    {
        target.Add(new WorkspaceTelemetrySegment
        {
            Source = source,
            LineText = input.LineText,
            CockpitShort = input.CockpitShort,
            IsBuildRunning = source == WorkspaceTelemetrySource.Build && input.IsBuildRunning,
        });
    }
}
