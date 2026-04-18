#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Composition;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.EnvironmentReadiness;

/// <summary>
/// Surface compositor contract for Environment Readiness page.
/// </summary>
public interface IEnvironmentReadinessSurfaceCompositor : ISurfaceCompositor<ObservableCollection<AnnunciatorLampItem>, IReadOnlyList<AnnunciatorLampItem>, EnvironmentReadinessSurfaceDecision, ObservableCollection<AnnunciatorLampItem>>
{
}
