namespace CascadeIDE.Features.Editor.Application.Monaco;

using CascadeIDE.Features.Editor.Presentation;
using CascadeIDE.Services;

public interface ICideEditorCapabilityRouter
{
    bool CanHandle(CideEditorInboundMessage message);

    Task HandleAsync(CideEditorInboundMessage message, MonacoEditorCapabilityContext context, CancellationToken cancellationToken = default);
}

/// <summary>Per-document session for capability routing (ADR 0163).</summary>
public sealed class MonacoEditorCapabilityContext
{
    public required MonacoEditorHostControl Host { get; init; }

    public required string FilePath { get; init; }

    public required Func<string> GetEditorText { get; init; }

    public required CSharpLanguageService CSharpLanguage { get; init; }

    public required WorkspaceDiagnosticsCoordinator WorkspaceDiagnostics { get; init; }

    public required Func<string, string, int, int, CancellationToken, Task<string?>> ResolveQuickInfoAsync { get; init; }

    public bool LspReady { get; init; }
}
