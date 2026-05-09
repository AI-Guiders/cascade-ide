namespace CascadeIDE.Contracts;

/// <summary>
/// Тип подписывается на события домена через интерфейс шины данных IDE (<c>IDataBus</c>) или через helper, который делает <c>Subscribe</c>.
/// </summary>
/// <remarks>Поиск: <c>[DataBusSubscriber]</c>. См. также <see cref="DataBusPublisherAttribute"/>.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = false,
    AllowMultiple = false)]
public sealed class DataBusSubscriberAttribute : Attribute
{
    public DataBusSubscriberAttribute()
    {
    }

    public DataBusSubscriberAttribute(string note) => Note = note;

    public string? Note { get; }
}
