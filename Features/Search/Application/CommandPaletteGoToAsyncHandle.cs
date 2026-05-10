#nullable enable

namespace CascadeIDE.Features.Search.Application;

/// <summary>Отмена и порядковый номер для асинхронного go-to (ripgrep) в палитре.</summary>
internal sealed class CommandPaletteGoToAsyncHandle
{
    public CancellationTokenSource? Cts;
    public long Seq;

    public void Cancel()
    {
        Cts?.Cancel();
        Cts = null;
    }
}
