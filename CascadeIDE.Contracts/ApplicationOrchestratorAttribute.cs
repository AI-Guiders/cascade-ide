namespace CascadeIDE.Contracts;

/// <summary>
/// Маркер оркестратора на слое Application: координация сценария (MCP, каналы, VM, фон/UI), а не узкая свёртка данных.
/// </summary>
/// <remarks>
/// Пара с <see cref="ComputingUnitAttribute"/>: CU — проекции и правила без сшивки сценария; оркестратор — точка координации.
/// Типичное место — <c>Features/*/Application</c>, суффикс имени <c>Orchestrator</c>.
///
/// Поиск: <c>[ApplicationOrchestrator]</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = false,
    AllowMultiple = false)]
public sealed class ApplicationOrchestratorAttribute : Attribute
{
    /// <summary>Создать маркер без текста-подсказки.</summary>
    public ApplicationOrchestratorAttribute()
    {
    }

    /// <summary>Создать маркер с необязательной подсказкой для обзора.</summary>
    public ApplicationOrchestratorAttribute(string note) => Note = note;

    /// <summary>Краткое текстовое уточнение для обзора (не семантика исполнения).</summary>
    public string? Note { get; }
}
