#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.EnvironmentReadiness;

/// <summary>
/// Composes Environment Readiness rows into UI-bound collection.
/// </summary>
public sealed class EnvironmentReadinessSurfaceCompositor : IEnvironmentReadinessSurfaceCompositor
{
    public ObservableCollection<EnvironmentReadinessItem> Compose(
        ObservableCollection<EnvironmentReadinessItem> scene,
        IReadOnlyList<EnvironmentReadinessItem> payload,
        in EnvironmentReadinessSurfaceDecision decision)
    {
        if (!decision.Enabled)
            return scene;

        scene.Clear();
        foreach (var row in payload)
            scene.Add(row);
        return scene;
    }
}
