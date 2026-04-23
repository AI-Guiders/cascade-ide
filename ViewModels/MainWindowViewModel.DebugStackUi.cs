namespace CascadeIDE.ViewModels;

/// <summary>Выбор кадра в панели «Стек» Mfd: подгрузка Locals для выбранного кадра (DAP).</summary>
public partial class MainWindowViewModel
{
    private int _debugStackSelectedIndex = -1;
    private bool _suppressDebugStackSelectedIndex;

    /// <summary>Индекс в <see cref="Features.Instrumentation.InstrumentationPanelViewModel.DebugStackFrames"/>; синхронизируется со снимком DAP.</summary>
    public int DebugStackSelectedIndex
    {
        get => _debugStackSelectedIndex;
        set
        {
            if (_debugStackSelectedIndex == value)
                return;
            _debugStackSelectedIndex = value;
            OnPropertyChanged();
            if (_suppressDebugStackSelectedIndex)
                return;
            if (value < 0)
                return;
            _ = RequestDebugFrameLocalsAsync(value);
        }
    }

    async Task RequestDebugFrameLocalsAsync(int frameIndex)
    {
        try
        {
            await _dapDebug.SetVariablesFrameIndexAsync(frameIndex, default).ConfigureAwait(true);
        }
        catch
        {
            // DAP/сессия — best-effort
        }
    }
}
