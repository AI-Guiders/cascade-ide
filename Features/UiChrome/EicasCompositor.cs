using System.Collections.ObjectModel;

namespace CascadeIDE.Features.UiChrome;

/// <summary>Сортировка и сборка коллекции для UI EICAS (вариант A: отдельно от <see cref="WorkspaceTelemetryCompositor"/>).</summary>
public static class EicasCompositor
{
    /// <summary>Порядок: Warning → Caution → Advisory; внутри уровня — по времени (старше раньше), без времени — по тексту.</summary>
    public static void Rebuild(ObservableCollection<EicasMessage> target, IReadOnlyList<EicasMessage> source)
    {
        target.Clear();
        foreach (var m in source.OrderBy(Ordinal).ThenBy(m => m.CreatedUtc ?? DateTimeOffset.MinValue).ThenBy(m => m.Text, StringComparer.Ordinal))
            target.Add(m);
    }

    private static int Ordinal(EicasMessage m) =>
        m.Severity switch
        {
            EicasSeverity.Warning => 0,
            EicasSeverity.Caution => 1,
            _ => 2,
        };
}
