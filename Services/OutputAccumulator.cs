using System.Text;

namespace CascadeIDE.Services;

/// <summary>
/// Accumulates text output while keeping only the last N characters.
/// Helps avoid OOM when external tools produce huge logs.
/// </summary>
internal sealed class OutputAccumulator
{
    private readonly object _gate = new();
    private readonly int _maxChars;
    private readonly Queue<string> _chunks = new();
    private int _totalChars;
    private bool _truncated;

    public OutputAccumulator(int maxChars)
    {
        _maxChars = Math.Max(1_024, maxChars);
    }

    public void Append(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
            return;

        lock (_gate)
        {
            if (span.Length >= _maxChars)
            {
                _chunks.Clear();
                _totalChars = 0;
                _chunks.Enqueue(new string(span[^_maxChars..]));
                _totalChars = _maxChars;
                _truncated = true;
                return;
            }

            _chunks.Enqueue(new string(span));
            _totalChars += span.Length;

            while (_totalChars > _maxChars && _chunks.Count > 0)
            {
                var removed = _chunks.Dequeue();
                _totalChars -= removed.Length;
                _truncated = true;
            }
        }
    }

    public string ToStringAndTrim()
    {
        lock (_gate)
        {
            if (_chunks.Count == 0)
                return "";

            var sb = new StringBuilder(_totalChars + (_truncated ? 32 : 0));
            if (_truncated)
                sb.Append("…(truncated)\r\n");

            foreach (var c in _chunks)
                sb.Append(c);

            return sb.ToString().Trim();
        }
    }
}

