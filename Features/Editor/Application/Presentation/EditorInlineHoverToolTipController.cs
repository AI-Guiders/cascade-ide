using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit;

namespace CascadeIDE.Features.Editor.Application.Presentation;

/// <summary>
/// Inline hover: debounce + приоритет диагностики hit-test, затем Quick Info (ADR 0085).
/// Координирует <see cref="ToolTip"/> на <see cref="TextEditor"/>; DAL/VM — только через колбэки.
/// </summary>
public sealed class EditorInlineHoverToolTipController : IDisposable
{
    private readonly TextEditor _editor;
    private readonly Func<string?> _getFilePath;
    private readonly Func<IReadOnlyList<EditorDiagnosticStrip>> _getStrips;
    private readonly Func<string, string, int, int, CancellationToken, Task<string?>> _getQuickInfoAsync;
    private readonly Func<string, string, int, int, string?> _getQuickInfoSync;
    private readonly Func<string, bool> _isQuickInfoLanguageFile;

    private Point _lastPointerInTextView;
    private string? _lastTipText;
    private int _tooltipSeq;
    private readonly DispatcherTimer _debounce;
    private bool _disposed;

    public EditorInlineHoverToolTipController(
        TextEditor editor,
        TimeSpan debounce,
        Func<string?> getFilePath,
        Func<IReadOnlyList<EditorDiagnosticStrip>> getStrips,
        Func<string, string, int, int, CancellationToken, Task<string?>> getQuickInfoAsync,
        Func<string, string, int, int, string?> getQuickInfoSync,
        Func<string, bool> isQuickInfoLanguageFile)
    {
        _editor = editor;
        _getFilePath = getFilePath;
        _getStrips = getStrips;
        _getQuickInfoAsync = getQuickInfoAsync;
        _getQuickInfoSync = getQuickInfoSync;
        _isQuickInfoLanguageFile = isQuickInfoLanguageFile;

        _debounce = new DispatcherTimer { Interval = debounce };
        _debounce.Tick += OnDebounceTick;
    }

    public void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        _lastPointerInTextView = e.GetPosition(_editor.TextArea.TextView);
        _debounce.Stop();
        _debounce.Start();
    }

    public void OnPointerExited(object? sender, PointerEventArgs e)
    {
        ToolTip.SetTip(_editor, null);
        _lastTipText = null;
        _tooltipSeq++;
    }

    public void StopDebounce() => _debounce.Stop();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _debounce.Stop();
        _debounce.Tick -= OnDebounceTick;
    }

    private void OnDebounceTick(object? sender, EventArgs e)
    {
        _debounce.Stop();
        UpdateDiagnosticToolTip();
    }

    private void UpdateDiagnosticToolTip()
    {
        var path = _getFilePath();
        if (string.IsNullOrEmpty(path))
        {
            ToolTip.SetTip(_editor, null);
            return;
        }

        if (!_isQuickInfoLanguageFile(path))
        {
            ToolTip.SetTip(_editor, null);
            return;
        }

        var seq = ++_tooltipSeq;

        var tv = _editor.TextArea.TextView;
        var pos = tv.GetPosition(_lastPointerInTextView);
        if (pos is null)
        {
            if (_lastTipText is not null)
            {
                ToolTip.SetTip(_editor, null);
                _lastTipText = null;
            }

            return;
        }

        int offset;
        try
        {
            offset = _editor.Document.GetOffset(pos.Value.Line, pos.Value.Column);
        }
        catch
        {
            ToolTip.SetTip(_editor, null);
            return;
        }

        var text = _editor.Document.Text ?? "";
        var strips = _getStrips();
        var hit = WorkspaceDiagnosticsCoordinator.HitTestForToolTip(
            strips,
            offset,
            pos.Value.Line,
            pos.Value.Column,
            text);
        if (hit is not null)
        {
            var tip = $"{hit.Id}: {hit.Message}";
            if (seq != _tooltipSeq)
                return;
            if (string.Equals(tip, _lastTipText, StringComparison.Ordinal))
                return;
            _lastTipText = tip;
            ToolTip.SetTip(_editor, tip);
            return;
        }

        var line = pos.Value.Line;
        var col = pos.Value.Column;
        _ = Task.Run(async () =>
        {
            string? q;
            try
            {
                q = await _getQuickInfoAsync(path, text, line, col, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                q = _getQuickInfoSync(path, text, line, col);
            }

            UiScheduler.Default.Post(() =>
            {
                if (seq != _tooltipSeq)
                    return;
                if (string.IsNullOrEmpty(q))
                {
                    if (_lastTipText is not null)
                    {
                        ToolTip.SetTip(_editor, null);
                        _lastTipText = null;
                    }

                    return;
                }

                if (string.Equals(q, _lastTipText, StringComparison.Ordinal))
                    return;
                _lastTipText = q;
                ToolTip.SetTip(_editor, q);
            });
        });
    }
}
