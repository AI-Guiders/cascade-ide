namespace CascadeIDE.Services.Lsp;

/// <summary>
/// Контракт источника диагностик с C# language server (LSP <c>textDocument/publishDiagnostics</c>).
/// In-process Roslyn сейчас идёт через <see cref="WorkspaceDiagnosticsCoordinator"/>; позже можно
/// реализовать этот интерфейс поверх OmniSharp-клиента и объединять полосы в одном координаторе.
/// </summary>
public interface ILspDiagnosticSource : IDisposable
{
    /// <summary>Handshake завершён и процесс жив (иначе координатор падает обратно на парсер).</summary>
    bool IsActive { get; }

    /// <summary>Обновились диагностики хотя бы по одному документу.</summary>
    event Action? DiagnosticsChanged;

    /// <summary>Диагностики для открытого документа (путь как в редакторе).</summary>
    IReadOnlyList<EditorDiagnosticStrip> GetStripsForFile(string? filePath);
}
