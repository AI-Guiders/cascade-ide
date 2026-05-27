using CascadeIDE.Services;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>
/// MLP-мост AEE ↔ Roslyn batch (ADR 0148 §5, W6): единая точка для будущего
/// <c>GetDiagnosticsBatch</c> / <c>ApplyPatchBatch</c> без дублирования MCP.
/// </summary>
public sealed class AgentAeeRoslynBridge
{
    private readonly CSharpLanguageService? _language;

    public AgentAeeRoslynBridge(CSharpLanguageService? language) => _language = language;

    public bool IsAvailable => _language is not null;

    /// <summary>Сейчас делегирует в L0 runner; идеал — один IPC round-trip на шаг.</summary>
    public Task<L0DiagnosticsOutcome> DiagnoseOpenAndDirtyAsync(
        AgentRoslynL0Diagnostics l0,
        CancellationToken cancellationToken = default) =>
        l0.RunAsync(cancellationToken);
}
