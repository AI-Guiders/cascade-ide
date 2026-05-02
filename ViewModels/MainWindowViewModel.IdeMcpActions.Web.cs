#nullable enable

namespace CascadeIDE.ViewModels;

/// <summary>Реализация <see cref="Services.IIdeMcpActions"/>: публичный веб-запрос (DuckDuckGo Instant Answer).</summary>
partial class MainWindowViewModel
{
    Task<string> Services.IIdeMcpActions.SearchWebPublicQueryAsync(string query, CancellationToken cancellationToken) =>
        Services.WebPublicSearchClient.FetchDdgInstantAnswerJsonAsync(query, cancellationToken);

    Task<string> Services.IIdeMcpActions.FetchWebPublicUrlAsync(string url, int? maxChars, CancellationToken cancellationToken) =>
        Services.WebPublicDocumentFetchClient.FetchAsync(url, maxChars, cancellationToken);
}
