#nullable enable

namespace CascadeIDE.Models.Editor;

/// <summary>Номер колонки в терминах редактора и IDE-команд: <b>1-based</b>, минимум 1 (перед первым символом строки = 1).</summary>
public readonly struct ColumnNumber : IEquatable<ColumnNumber>, IComparable<ColumnNumber>
{
    public const int MinimumOneBasedInclusive = 1;

    public int Value { get; }

    private ColumnNumber(int value) => Value = value;

    /// <summary>Создать из сырого int; отказ, если значение &lt; <see cref="MinimumOneBasedInclusive"/>.</summary>
    public static bool TryCreate(int raw, out ColumnNumber columnNumber)
    {
        if (raw < MinimumOneBasedInclusive)
        {
            columnNumber = default;
            return false;
        }

        columnNumber = new ColumnNumber(raw);
        return true;
    }

    public bool Equals(ColumnNumber other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is ColumnNumber other && Equals(other);

    public override int GetHashCode() => Value;

    public int CompareTo(ColumnNumber other) => Value.CompareTo(other.Value);

    public static bool operator ==(ColumnNumber left, ColumnNumber right) => left.Equals(right);

    public static bool operator !=(ColumnNumber left, ColumnNumber right) => !left.Equals(right);

    public static bool operator <(ColumnNumber left, ColumnNumber right) => left.CompareTo(right) < 0;

    public static bool operator <=(ColumnNumber left, ColumnNumber right) => left.CompareTo(right) <= 0;

    public static bool operator >(ColumnNumber left, ColumnNumber right) => left.CompareTo(right) > 0;

    public static bool operator >=(ColumnNumber left, ColumnNumber right) => left.CompareTo(right) >= 0;
}
