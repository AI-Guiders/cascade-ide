#nullable enable

namespace CascadeIDE.Models.Editor;

/// <summary>Закрытый 1-based диапазон строк редактора: <see cref="Start"/> ≤ <see cref="End"/> (инклюзивно с обеих сторон).</summary>
public readonly struct LineRange : IEquatable<LineRange>
{
    public LineNumber Start { get; }

    public LineNumber End { get; }

    private LineRange(LineNumber start, LineNumber end)
    {
        Start = start;
        End = end;
    }

    /// <summary>Отказ, если <paramref name="end"/> &lt; <paramref name="start"/>.</summary>
    public static bool TryCreate(LineNumber start, LineNumber end, out LineRange range)
    {
        if (end.Value < start.Value)
        {
            range = default;
            return false;
        }

        range = new LineRange(start, end);
        return true;
    }

    public bool Equals(LineRange other) => Start.Equals(other.Start) && End.Equals(other.End);

    public override bool Equals(object? obj) => obj is LineRange other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(LineRange left, LineRange right) => left.Equals(right);

    public static bool operator !=(LineRange left, LineRange right) => !left.Equals(right);
}
