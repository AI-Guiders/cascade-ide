using Avalonia.Threading;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Build;

/// <summary>
/// Вкладка «Сборка · вывод» нижней панели: текст вывода сборки и связанных операций.
/// <see cref="Append"/> коалесит обновления привязки через один <see cref="IUiScheduler.Post"/> на серию вызовов (фаза 5).
/// </summary>
public partial class BuildOutputPanelViewModel : ViewModelBase
{
    /// <summary>
    /// Максимальный размер накопленного вывода (чтобы не ловить OOM на больших логах).
    /// </summary>
    public const int MaxChars = 250_000;

    private readonly object _batchLock = new();
    private OutputAccumulator _acc = new(MaxChars);
    private int _contentVersion;
    private bool _flushPosted;

    [ObservableProperty]
    private string _buildOutput = "";

    public void Clear()
    {
        lock (_batchLock)
        {
            _contentVersion++;
            _acc = new OutputAccumulator(MaxChars);
            _flushPosted = false;
            BuildOutput = "";
        }
    }

    public void Set(string text)
    {
        lock (_batchLock)
        {
            _contentVersion++;
            _acc = new OutputAccumulator(MaxChars);
            _acc.Append((text ?? "").AsSpan());
            _flushPosted = false;
            BuildOutput = _acc.ToStringAndTrim();
        }
    }

    public void Append(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        int ver;
        lock (_batchLock)
        {
            _acc.Append(text.AsSpan());
            if (_flushPosted)
                return;
            _flushPosted = true;
            ver = _contentVersion;
        }

        UiScheduler.Default.Post(() => FlushToUi(ver), DispatcherPriority.Background);
    }

    private void FlushToUi(int scheduledVersion)
    {
        lock (_batchLock)
        {
            if (scheduledVersion != _contentVersion)
            {
                _flushPosted = false;
                return;
            }

            BuildOutput = _acc.ToStringAndTrim();
            _flushPosted = false;
        }
    }

    /// <summary>
    /// Синхронно выставляет <see cref="BuildOutput"/> из накопителя и отменяет отложенный flush
    /// (после серии <see cref="Append"/>, чтобы MCP/пользователь не читали устаревший текст).
    /// </summary>
    public void FlushPending()
    {
        lock (_batchLock)
        {
            _contentVersion++;
            BuildOutput = _acc.ToStringAndTrim();
            _flushPosted = false;
        }
    }
}
