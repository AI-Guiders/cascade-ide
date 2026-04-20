namespace CascadeIDE.Cockpit.Channels.Eicas;

/// <summary>EICAS / CAS уровни (ADR 0021 §5): Warning &gt; Caution &gt; Advisory.</summary>
/// <remarks>Согласованы по смыслу с <see cref="CascadeIDE.Models.AnnunciatorLampLevel"/> (другой тип — чтобы не тащить кокпит в модели ламп-ячейки).</remarks>
public enum EicasSeverity
{
    Advisory,
    Caution,
    Warning,
}
