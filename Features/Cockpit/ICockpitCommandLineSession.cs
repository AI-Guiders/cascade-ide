#nullable enable

using CascadeIDE.Features.Chat;

namespace CascadeIDE.Features.Cockpit;

/// <summary>Сессия Cockpit Command Line (ADR 0138). Буфер CCL отделён от composer.</summary>
public interface ICockpitCommandLineSession
{
    bool IsOpen { get; }

    CockpitCommandLineHostKind ActiveHost { get; }

    string BufferText { get; set; }

    int CaretIndex { get; set; }

    string? PreviewSummary { get; }

    SlashCommandPreviewKind PreviewKind { get; }

    /// <summary>Текст для ToolTip / screen reader, когда pill не несёт полное сообщение.</summary>
    string? PreviewAccessibilityToolTip { get; }

    void Open(CockpitCommandLineHostKind? host = null, string initialText = "/");

    void Close();

    void RefreshPreview();

    Task<CockpitCommandLineCommitResult> TryCommitAsync(CancellationToken cancellationToken = default);
}
