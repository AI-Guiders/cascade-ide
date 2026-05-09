#nullable enable

using CascadeIDE.Features.IdeMcp.Application;

namespace CascadeIDE.ViewModels;

/// <summary>Реализация <see cref="Services.IIdeMcpActions"/>: публичный веб-запрос (DuckDuckGo Instant Answer) и загрузка публичного URL.</summary>
public partial class MainWindowViewModel
{
    Task<string> Services.IIdeMcpActions.SearchWebPublicQueryAsync(string query, CancellationToken cancellationToken) =>
        IdeMcpWebPublicOrchestrator.SearchPublicQueryAsync(query, cancellationToken);

    Task<string> Services.IIdeMcpActions.FetchWebPublicUrlAsync(string url, int? maxChars, CancellationToken cancellationToken) =>
        IdeMcpWebPublicOrchestrator.FetchPublicUrlAsync(url, maxChars, cancellationToken);
}
