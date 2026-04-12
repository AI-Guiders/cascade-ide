using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.Eicas;

namespace CascadeIDE.Cockpit.Composition.Eicas;

/// <summary>
/// Композитор поверхности для канала EICAS (ADR 0036 п.3): упорядочивает <see cref="EicasMessage"/> по серьёзности и времени
/// перед привязкой к полосе/представлению. Отделён от <see cref="WorkspaceHealthSegmentBuilder"/> (композитор канала Workspace Health).
/// </summary>
public static class EicasMessageSorter
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
