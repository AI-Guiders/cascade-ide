namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Live snapshot for <see cref="MonacoWebViewSurfaceAdapter"/>.</summary>
public sealed class MonacoEditorSessionState
{
    private readonly object _gate = new();

    public int Version { get; private set; }
    public string Text { get; private set; } = "";
    public int CaretOffset { get; private set; }
    public int SelectionStart { get; private set; }
    public int SelectionLength { get; private set; }

    public void ApplyInbound(CideEditorInboundMessage msg)
    {
        lock (_gate)
        {
            if (msg.Version is int v)
                Version = v;
            if (msg.Text is not null)
                Text = msg.Text;
            if (msg.CaretOffset is int caret)
                CaretOffset = caret;
            if (msg.SelectionStart is int selStart)
                SelectionStart = selStart;
            if (msg.SelectionLength is int selLen)
                SelectionLength = selLen;
        }
    }

    public void Seed(string text, int version)
    {
        lock (_gate)
        {
            Text = text;
            Version = version;
            CaretOffset = 0;
            SelectionStart = 0;
            SelectionLength = 0;
        }
    }

    public void ReadSnapshot(out int version, out string text, out int caret, out int selStart, out int selLen)
    {
        lock (_gate)
        {
            version = Version;
            text = Text;
            caret = CaretOffset;
            selStart = SelectionStart;
            selLen = SelectionLength;
        }
    }
}

/// <summary><see cref="IEditorSurfaceAdapter"/> over Monaco WebView host (ADR 0162).</summary>
public sealed class MonacoWebViewSurfaceAdapter : IEditorSurfaceAdapter
{
    private readonly MonacoEditorSessionState _state;
    private readonly string? _filePath;

    public MonacoWebViewSurfaceAdapter(MonacoEditorSessionState state, string? filePath)
    {
        _state = state;
        _filePath = filePath;
    }

    public string? FilePath => _filePath;

    public int CaretOffset
    {
        get
        {
            _state.ReadSnapshot(out _, out _, out var caret, out _, out _);
            return caret;
        }
    }

    public int TextLength
    {
        get
        {
            _state.ReadSnapshot(out _, out var text, out _, out _, out _);
            return text.Length;
        }
    }

    public void GetSelection(out int start, out int length)
    {
        _state.ReadSnapshot(out _, out _, out _, out start, out length);
    }
}
