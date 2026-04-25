#nullable enable

namespace CascadeIDE.Cockpit.ComputingUnits;

/// <summary>
/// Вычислительный блок кабины (cockpit compute unit, <strong>CCU</strong>): явная архитектурная граница между
/// транспортом/сырьём и снимком или шагом свёртки для канала. См. ADR 0097. Контракт намеренно пустой — поведение задаёт
/// конкретный тип; маркер нужен для <c>is</c>, ревью и будущих CASCOPE-анализаторов.
/// </summary>
public interface ICockpitComputeUnit
{
}

/// <summary>
/// Тип полезной нагрузки на границе CCU: нормализованный снимок или фрагмент, который **не** исполняет свёртку, но участвует
/// в цепочке ADR 0097 до канала. Отдельно от <see cref="ICockpitComputeUnit"/>, чтобы не путать DTO с «юнитом» в смысле LRU.
/// </summary>
public interface ICockpitComputeUnitPayload
{
}
