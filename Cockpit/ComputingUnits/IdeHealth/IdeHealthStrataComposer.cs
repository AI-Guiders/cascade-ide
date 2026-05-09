using CascadeIDE.Contracts;

namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// Одна точка композиции снимка по стратам (ADR 0095 / ADR 0097): <see cref="IdeHealthWorkspaceInput"/> +
/// <see cref="IdeHealthSolutionInput"/> + <see cref="IdeHealthIdeHostInput"/> → <see cref="IdeHealthInputSnapshot"/>.
/// </summary>
[ComputingUnit]
public static class IdeHealthStrataComposer
{
    /// <summary>Собирает снимок; <paramref name="ideHost"/> по умолчанию пустой (страт C пока не даёт сегментов в полосе).</summary>
    public static IdeHealthInputSnapshot Compose(
        IdeHealthWorkspaceInput workspace,
        IdeHealthSolutionInput solution,
        IdeHealthIdeHostInput ideHost = default) =>
        new(workspace, solution, ideHost);
}
