namespace CascadeIDE.Models.Shell;

/// <summary>
/// Именованный расклад колонки Pfd (какой набор/компоновка инструментов в регионе; ADR 0088). Не «страница» оболочки и не <see cref="Cockpit.Composition.HostSurface.CockpitSlotIds.Pfd"/> сам по себе.
/// v1 — один layout; позже — декларативные пресеты.
/// </summary>
public interface IPfdLayout
{
    /// <summary>Стабильный id расклада (логи, TOML, снапшоты).</summary>
    string LayoutId { get; }
}
