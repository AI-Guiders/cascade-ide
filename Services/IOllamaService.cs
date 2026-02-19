namespace AgentIde.Services;

public interface IOllamaService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetModelNamesAsync(CancellationToken cancellationToken = default);
}
