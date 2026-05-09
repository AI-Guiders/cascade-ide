namespace CascadeIDE.Contracts;

/// <summary>
/// Граница ввода-вывода и внешней среды: диск, процессы, сеть, host OS, LSP-транспорт — не чистая проекция домена.
/// </summary>
/// <remarks>
/// Типично <c>Features/*/DataAcquisition</c> и явные DAL-реализации. Пара к <see cref="ComputingUnitAttribute"/> (логика без I/O).
///
/// Поиск: <c>[IoBoundary]</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
    Inherited = false,
    AllowMultiple = false)]
public sealed class IoBoundaryAttribute : Attribute
{
    public IoBoundaryAttribute()
    {
    }

    /// <param name="note">Подсказка (подсистема: git, lsp, filesystem, …).</param>
    public IoBoundaryAttribute(string note) => Note = note;

    public string? Note { get; }
}
