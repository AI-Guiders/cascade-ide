using System.Collections.ObjectModel;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Собирает единый упорядоченный список сегментов полосы телеметрии из уже вычисленных строк
/// (<see cref="ViewModels.MainWindowViewModel"/> / <see cref="UiChromeViewModel"/>).
/// Единая точка порядка и состава — чтобы не плодить отдельные привязки на каждый источник.
/// </summary>
public static class AttentionStripCompositor
{
    /// <summary>Порядок сегментов на полосе: сборка → тесты → отладка → git.</summary>
    public static void Rebuild(
        ObservableCollection<AttentionStripSegment> target,
        string buildLine,
        string buildShort,
        bool isBuildRunning,
        string testsLine,
        string testsShort,
        string debugLine,
        string debugShort,
        string gitLine,
        string gitShort)
    {
        target.Clear();
        target.Add(new AttentionStripSegment
        {
            Source = AttentionStripSource.Build,
            LineText = buildLine,
            CockpitShort = buildShort,
            IsBuildRunning = isBuildRunning,
        });
        target.Add(new AttentionStripSegment
        {
            Source = AttentionStripSource.Tests,
            LineText = testsLine,
            CockpitShort = testsShort,
        });
        target.Add(new AttentionStripSegment
        {
            Source = AttentionStripSource.Debug,
            LineText = debugLine,
            CockpitShort = debugShort,
        });
        target.Add(new AttentionStripSegment
        {
            Source = AttentionStripSource.Git,
            LineText = gitLine,
            CockpitShort = gitShort,
        });
    }
}
