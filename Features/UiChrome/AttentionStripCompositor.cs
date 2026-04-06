using System.Collections.ObjectModel;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Собирает единый упорядоченный список сегментов полосы телеметрии из уже вычисленных строк
/// (<see cref="ViewModels.MainWindowViewModel"/> / <see cref="UiChromeViewModel"/>).
/// Единая точка порядка и состава (ADR 0021 / идея «одного композитора стекла»); новые источники — через
/// <see cref="AttentionStripInputSnapshot"/>, а не отдельные параметры на каждый вызов.
/// </summary>
public static class AttentionStripCompositor
{
    /// <summary>Порядок сегментов на полосе: сборка → тесты → отладка → git.</summary>
    public static void Rebuild(ObservableCollection<AttentionStripSegment> target, AttentionStripInputSnapshot inputs)
    {
        target.Clear();
        Append(target, AttentionStripSource.Build, inputs.Build);
        Append(target, AttentionStripSource.Tests, inputs.Tests);
        Append(target, AttentionStripSource.Debug, inputs.Debug);
        Append(target, AttentionStripSource.Git, inputs.Git);
    }

    private static void Append(
        ObservableCollection<AttentionStripSegment> target,
        AttentionStripSource source,
        AttentionStripSegmentInput input)
    {
        target.Add(new AttentionStripSegment
        {
            Source = source,
            LineText = input.LineText,
            CockpitShort = input.CockpitShort,
            IsBuildRunning = source == AttentionStripSource.Build && input.IsBuildRunning,
        });
    }
}
