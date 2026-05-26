#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Режим хвоста аргументов после канонического slash-пути (ADR 0150).</summary>
public enum SlashArgTailKind
{
    None = 0,
    Optional = 1,
    Required = 2,
}
