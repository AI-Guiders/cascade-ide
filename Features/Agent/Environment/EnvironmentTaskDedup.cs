namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Coalesce duplicate environment tasks within window (ADR 0148 §8).</summary>
public sealed class EnvironmentTaskDedup
{
    private readonly int _coalesceWindowMs;
    private readonly object _gate = new();
    private string? _lastKey;
    private DateTimeOffset _lastAtUtc;

    public EnvironmentTaskDedup(int coalesceWindowMs) => _coalesceWindowMs = Math.Max(0, coalesceWindowMs);

    public bool ShouldCoalesce(string dedupKey)
    {
        if (_coalesceWindowMs <= 0)
            return false;

        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (_lastKey == dedupKey && (now - _lastAtUtc).TotalMilliseconds < _coalesceWindowMs)
                return true;

            _lastKey = dedupKey;
            _lastAtUtc = now;
            return false;
        }
    }
}
