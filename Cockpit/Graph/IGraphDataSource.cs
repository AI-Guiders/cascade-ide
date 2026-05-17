#nullable enable

namespace CascadeIDE.Cockpit.Graph;

/// <summary>
/// Источник wire JSON для graph-backed прибора (CDS, не IDS). ADR 0115; доменный снимок — <see cref="GraphDocument"/> (ADR 0067).
/// </summary>
public interface IGraphDataSource
{
    string BuildNavigationJson(GraphNavigationJsonRequest request);
}

/// <summary>
/// Источник, возвращающий <see cref="GraphDocument"/> напрямую (предпочтительно для нового кода).
/// </summary>
public interface IGraphDocumentSource
{
    bool TryBuildDocument(GraphNavigationJsonRequest request, out GraphDocument? document, out string? wireJson);
}
