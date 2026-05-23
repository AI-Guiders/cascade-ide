#nullable enable

using CascadeIDE.Features.Chat;

namespace CascadeIDE.Features.Cockpit;

/// <summary>Реализация <see cref="ICockpitCommandLineSession"/> поверх <see cref="ChatPanelViewModel"/>.</summary>
internal sealed class CockpitCommandLineSession : ICockpitCommandLineSession
{
    private readonly ChatPanelViewModel _owner;
    private readonly SlashCommandPreviewService _previewService;
    private CockpitCommandLineHostKind _activeHost = CockpitCommandLineHostKind.Intercom;

    public CockpitCommandLineSession(ChatPanelViewModel owner, SlashCommandPreviewService previewService)
    {
        _owner = owner;
        _previewService = previewService;
    }

    public bool IsOpen => _owner.IsCockpitCommandLineOpen;

    public CockpitCommandLineHostKind ActiveHost => _activeHost;

    public string BufferText
    {
        get => _owner.CockpitCommandLineText;
        set => _owner.CockpitCommandLineText = value;
    }

    public int CaretIndex
    {
        get => _owner.CockpitCommandLineCaretIndex;
        set => _owner.CockpitCommandLineCaretIndex = value;
    }

    public string? PreviewSummary =>
        string.IsNullOrWhiteSpace(_owner.CommandLineSlashPreview) ? null : _owner.CommandLineSlashPreview;

    public SlashCommandPreviewKind PreviewKind => _owner.CommandLineSlashPreviewKind;

    public string? PreviewAccessibilityToolTip => _owner.CommandLineSlashPreviewToolTip;

    public void Open(CockpitCommandLineHostKind? host = null, string initialText = "/")
    {
        _activeHost = host ?? CockpitCommandLineHostKind.Intercom;
        _owner.OpenCockpitCommandLine(initialText);
    }

    public void Close() => _owner.CloseCockpitCommandLine();

    public void RefreshPreview() => _owner.UpdateSlashPreview(TciInputSurface.CockpitCommandLine);

    public async Task<CockpitCommandLineCommitResult> TryCommitAsync(CancellationToken cancellationToken = default)
    {
        var success = await _owner.TryCommitCockpitCommandLineAsync(cancellationToken).ConfigureAwait(false);
        return new(true, success, null);
    }
}
