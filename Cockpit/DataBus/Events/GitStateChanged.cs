namespace CascadeIDE.Cockpit.DataBus;

/// <summary>Текст git-сегмента IDE Health (две строки: полоса и кольцо).</summary>
public readonly record struct GitStateChanged(string Line, string CockpitShort);
