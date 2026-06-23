using Microsoft.CodeAnalysis;

namespace CascadeIDE.Services;

/// <summary>Диагностика в документе: UTF-16 offset/length + 1-based line/column для Problems и Monaco squiggles.</summary>
public sealed record EditorDiagnosticStrip(
    int Start,
    int Length,
    DiagnosticSeverity Severity,
    string Id,
    string Message,
    int Line1,
    int Column1);
