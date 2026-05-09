namespace CascadeIDE.Contracts;

/// <summary>
/// Логика сознательно переводит выполнение на UI-поток или UI-scheduler (напр. <c>UiScheduler.Post/InvokeAsync</c>, <c>Dispatcher</c>).
/// </summary>
/// <remarks>
/// Маркер для точек, где неверный поток даёт гонки или исключения Avalonia. Не вешать на весь ViewModel «на всякий случай» —
/// только на узкие хелперы оркестрации UI.
///
/// Поиск: <c>[UiThreadMarshal]</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method,
    Inherited = false,
    AllowMultiple = false)]
public sealed class UiThreadMarshalAttribute : Attribute
{
    public UiThreadMarshalAttribute()
    {
    }

    public UiThreadMarshalAttribute(string note) => Note = note;

    public string? Note { get; }
}
