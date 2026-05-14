#nullable enable

namespace CascadeIDE.Cockpit.Graph;

/// <summary>
/// Источник JSON полезной нагрузки для graph-backed прибора карты навигации (CDS, не IDS). ADR 0115.
/// </summary>
public interface IGraphDataSource
{
    string BuildNavigationJson(CodeNavigationMapJsonRequest request);
}
