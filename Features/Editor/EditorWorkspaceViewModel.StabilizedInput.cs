using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Editor;

/// <summary>
/// Один hi-freq throttler на главное окно (ADR 0103): не N фоновых consumer на N вкладок.
/// Стабилизированный выход обрабатывается только если <see cref="EditorInputDelta.FilePath"/>
/// совпадает с <see cref="CurrentFilePath"/>.
/// </summary>
public sealed partial class EditorWorkspaceViewModel
{
    private EditorStabilizedInputThrottler? _editorStabilizedThrottler;
    private CancellationTokenSource? _editorStabilizedCts;
    private Action<EditorInputDelta>? _activeEditorStabilizedHudHandler;

    internal void SetActiveEditorStabilizedHudHandler(Action<EditorInputDelta>? handler)
    {
        _activeEditorStabilizedHudHandler = handler;
        EnsureEditorStabilizedInputStarted();
    }

    internal void ClearActiveEditorStabilizedHudHandlerIfEquals(Action<EditorInputDelta>? handler)
    {
        if (ReferenceEquals(_activeEditorStabilizedHudHandler, handler))
            _activeEditorStabilizedHudHandler = null;
    }

    internal bool TryPostEditorStabilizedInput(EditorInputDelta delta)
    {
        EnsureEditorStabilizedInputStarted();
        return _editorStabilizedThrottler!.TryPost(delta);
    }

    private void EnsureEditorStabilizedInputStarted()
    {
        if (_editorStabilizedThrottler is not null)
            return;
        _editorStabilizedCts = new CancellationTokenSource();
        _editorStabilizedThrottler = new EditorStabilizedInputThrottler(UiScheduler.Default, TimeSpan.FromMilliseconds(24));
        _editorStabilizedThrottler.Start(OnEditorStabilizedInput, _editorStabilizedCts.Token);
    }

    private void OnEditorStabilizedInput(EditorInputDelta d)
    {
        var path = CurrentFilePath;
        if (string.IsNullOrEmpty(d.FilePath) || string.IsNullOrEmpty(path)
            || !EditorTextCoordinateUtilities.PathsReferToSameFile(d.FilePath, path))
            return;

        _activeEditorStabilizedHudHandler?.Invoke(d);
        _host.HostUpdateCodeNavigationMapCaretOffset(d.CaretOffset);
    }

    internal void ShutdownEditorStabilizedInput()
    {
        _activeEditorStabilizedHudHandler = null;
        _editorStabilizedCts?.Cancel();
        if (_editorStabilizedThrottler is { } t)
        {
            try
            {
                t.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch
            {
            }
        }

        _editorStabilizedThrottler = null;
        _editorStabilizedCts?.Dispose();
        _editorStabilizedCts = null;
    }
}
