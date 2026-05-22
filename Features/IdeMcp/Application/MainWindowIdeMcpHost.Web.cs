using CascadeIDE.ViewModels;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{

    public Task<string> SearchWebPublicQueryAsync(string query, CancellationToken cancellationToken) =>
        IdeMcpWebPublicOrchestrator.SearchPublicQueryAsync(query, cancellationToken);

    public Task<string> FetchWebPublicUrlAsync(string url, int? maxChars, CancellationToken cancellationToken) =>
        IdeMcpWebPublicOrchestrator.FetchPublicUrlAsync(url, maxChars, cancellationToken);

}
