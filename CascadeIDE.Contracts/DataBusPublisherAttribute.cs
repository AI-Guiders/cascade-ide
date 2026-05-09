namespace CascadeIDE.Contracts;

/// <summary>
/// Тип публикует доменные события на шину данных IDE (<c>IDataBus.Publish</c>), а не только подписывается.
/// </summary>
/// <remarks>Поиск: <c>[DataBusPublisher]</c>. См. <see cref="DataBusSubscriberAttribute"/>.</remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = false,
    AllowMultiple = false)]
public sealed class DataBusPublisherAttribute : Attribute
{
    public DataBusPublisherAttribute()
    {
    }

    public DataBusPublisherAttribute(string note) => Note = note;

    public string? Note { get; }
}
