#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Inclusive 1-based диапазон (строка файла или ordinal gutter сообщения).</summary>
public readonly record struct ParametricIntRange(int Start, int End)
{
    public int InclusiveCount => End - Start + 1;
}
