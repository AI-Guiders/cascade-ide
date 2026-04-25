namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// Страта сигнала для IDE Health по ADR 0095: семантический уровень источника, не severity.
/// </summary>
public enum IdeHealthStratum
{
    Workspace,
    Solution,
    Ide
}
