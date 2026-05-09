namespace CascadeIDE.Contracts;

/// <summary>
/// Маркер вычислительной единицы приложения («computing unit»): узкая статическая проекция, политика или нормализация данных.
/// </summary>
/// <remarks>
/// Отличается от orchestrators / MCP-хэндлеров / подписчиков шины: нет координации внешних сервисов и долгоживущего состояния единицы.
/// Допустимы локальные операции над путями и простыми проверками ОС там, где это часть правила нормализации, а не сценария с побочными эффектами.
/// Типичные места — <c>Features/*/Application</c>.
///
/// Поиск по коду: <c>[ComputingUnit]</c>.
/// В будущем к маркеру можно подключить анализатор; сейчас это соглашение для человека и для grep.
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
