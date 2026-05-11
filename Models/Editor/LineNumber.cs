#nullable enable

namespace CascadeIDE.Models.Editor;

/// <summary>Номер строки в терминах редактора и IDE-команд: <b>1-based</b>, минимум 1.</summary>
public readonly struct LineNumber : IEquatable<LineNumber>, IComparable<LineNumber>
{
    /// <summary>Минимальный допустимый номер строки (как в UI редактора и в JSON args команд).</summary>
    public const int MinimumOneBasedInclusive = 1;

    public int Value { get; }

    private LineNumber(int value) => Value = value;

    /// <summary>Создать из сырого int; отказ, если значение &lt; <see cref="MinimumOneBasedInclusive"/>.</summary>
    public static bool TryCreate(int raw, out LineNumber lineNumber)
    {
        if (raw < MinimumOneBasedInclusive)
        {
            lineNumber = default;
            return false;
        }

        lineNumber = new LineNumber(raw);
        return true;
    }

    public bool Equals(LineNumber other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is LineNumber other && Equals(other);

    public override int GetHashCode() => Value;

    public int CompareTo(LineNumber other) => Value.CompareTo(other.Value);

    public static bool operator ==(LineNumber left, LineNumber right) => left.Equals(right);

    public static bool operator !=(LineNumber left, LineNumber right) => !left.Equals(right);

    public static bool operator <(LineNumber left, LineNumber right) => left.CompareTo(right) < 0;

    public static bool operator <=(LineNumber left, LineNumber right) => left.CompareTo(right) <= 0;

    public static bool operator >(LineNumber left, LineNumber right) => left.CompareTo(right) > 0;

    public static bool operator >=(LineNumber left, LineNumber right) => left.CompareTo(right) >= 0;
}
