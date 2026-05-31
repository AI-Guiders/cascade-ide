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

    /// <summary>Сейчас делегирует в diagnose.files runner; идеал — один IPC round-trip на шаг.</summary>
    public Task<DiagnoseFilesOutcome> DiagnoseOpenAndDirtyAsync(
        AgentRoslynDiagnoseFilesDiagnostics diagnoseFiles,
        CancellationToken cancellationToken = default) =>
        diagnoseFiles.RunAsync(cancellationToken);
}
