using System.Collections.ObjectModel;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Собирает единый упорядоченный список сегментов полосы Workspace Health из уже вычисленных строк
/// (<see cref="ViewModels.MainWindowViewModel"/> / <see cref="UiChromeViewModel"/>).
/// Единая точка порядка и состава (ADR 0021 / идея «одного композитора стекла»); новые источники — через
/// <see cref="WorkspaceHealthInputSnapshot"/>, а не отдельные параметры на каждый вызов.
/// </summary>
public static class WorkspaceHealthCompositor
{
    /// <summary>Порядок сегментов на полосе: сборка → тесты → отладка → git.</summary>
    public static void Rebuild(ObservableCollection<WorkspaceHealthSegment> target, WorkspaceHealthInputSnapshot inputs)
    {
        target.Clear();
        Append(target, WorkspaceHealthSource.Build, inputs.Build);
        Append(target, WorkspaceHealthSource.Tests, inputs.Tests);
        Append(target, WorkspaceHealthSource.Debug, inputs.Debug);
        Append(target, WorkspaceHealthSource.Git, inputs.Git);
    }

    private static void Append(
        ObservableCollection<WorkspaceHealthSegment> target,
        WorkspaceHealthSource source,
        WorkspaceHealthSegmentInput input)
    {
        target.Add(new WorkspaceHealthSegment
        {
            Source = source,
            LineText = input.LineText,
            CockpitShort = input.CockpitShort,
            IsBuildRunning = source == WorkspaceHealthSource.Build && input.IsBuildRunning,
        });
    }
}
