namespace CascadeIDE.Contracts;

/// <summary>
/// Маркер вычислительной единицы приложения («computing unit»): узкая статическая проекция, политика или нормализация данных.
/// </summary>
/// <remarks>
/// Отличается от <c>*Orchestrator*</c>, MCP-хэндлеров координации и тонких адаптеров «только подписка на шину без свёртки».
/// Допустимы локальные операции над путями и простыми проверками ОС там, где это часть правила нормализации, а не сценария с побочными эффектами.
///
/// Маркируются: статические CU в <c>Features/*/Application</c> (имя класса не оканчивается на <c>Orchestrator</c>),
/// типы-компьюторы кабины в <c>Cockpit/ComputingUnits</c> (типы с <c>: ICockpitComputeUnit</c> и статические CCU-хелперы в том дереве).
/// Не маркировать только-DTO (<c>record struct</c> полезная нагрузка), перечисления и пустые контрактные интерфейсы.
///
/// Поиск: <c>[ComputingUnit]</c>. Пара: <see cref="IoBoundaryAttribute"/> (I/O), <see cref="ApplicationOrchestratorAttribute"/> (координация), шина —
/// <see cref="DataBusSubscriberAttribute"/> / <see cref="DataBusPublisherAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = false,
    AllowMultiple = false)]
public sealed class ComputingUnitAttribute : Attribute
{
    /// <summary>Создать маркер без текста-подсказки.</summary>
    public ComputingUnitAttribute()
    {
    }

    /// <summary>Создать маркер с необязательной подсказкой для обзора (категория единицы).</summary>
    /// <param name="note">Краткая подсказка; не влияет на компиляцию.</param>
    public ComputingUnitAttribute(string note) => Note = note;

    /// <summary>Краткое текстовое уточнение для обзора (не семантика исполнения).</summary>
    public string? Note { get; }
}
