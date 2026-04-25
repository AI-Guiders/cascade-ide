namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// Масштаб сигнала внутри strata=solution по ADR 0095: общий по решению или по конкретному проекту.
/// Для strata=workspace/ide поле можно оставлять в значении по умолчанию.
/// </summary>
public enum IdeHealthScope
{
    Solution,
    Project
}
