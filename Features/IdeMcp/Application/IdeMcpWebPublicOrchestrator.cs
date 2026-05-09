using CascadeIDE.Contracts;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>Публичный поиск и загрузка документов по URL для MCP IDE.</summary>
[ApplicationOrchestrator]
public static class IdeMcpWebPublicOrchestrator
{
    public static Task<string> SearchPublicQueryAsync(string query, CancellationToken cancellationToken) =>
        WebPublicSearchClient.FetchDdgInstantAnswerJsonAsync(query, cancellationToken);

    public static Task<string> FetchPublicUrlAsync(string url, int? maxChars, CancellationToken cancellationToken) =>
        WebPublicDocumentFetchClient.FetchAsync(url, maxChars, cancellationToken);
}
