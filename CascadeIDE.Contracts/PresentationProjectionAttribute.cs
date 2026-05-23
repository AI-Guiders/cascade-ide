#nullable enable

namespace CascadeIDE.Contracts;

/// <summary>
/// Маркер проекции presentation-слоя: статическое преобразование снимка/DTO в строки и флаги для биндинга VM.
/// </summary>
/// <remarks>
/// Отличается от <see cref="ApplicationOrchestratorAttribute"/> (координация сценария, async, UI/MCP/DataBus)
/// и от CCU в <c>Cockpit/ComputingUnits</c> (<see cref="ComputingUnitAttribute"/> + <see cref="ICockpitComputeUnit"/>).
///
/// Типичное место — <c>Features/*/Application</c>, суффикс <c>*Projection</c> или <c>*PresentationProjection</c>.
/// Без I/O, без публикации в DataBus, без хранения состояния процесса.
///
/// Поиск: <c>[PresentationProjection]</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = false,
    AllowMultiple = false)]
public sealed class PresentationProjectionAttribute : Attribute
{
    public PresentationProjectionAttribute()
    {
    }

    public PresentationProjectionAttribute(string note) => Note = note;

    public string? Note { get; }
}
