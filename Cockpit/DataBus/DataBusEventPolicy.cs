namespace CascadeIDE.Cockpit.DataBus;

/// <summary>
/// Как маршрутизировать <see cref="TEvent"/> в async-режиме <see cref="InMemoryDataBus"/>:
/// <see cref="IsBurst"/> → bounded(1) + DropOldest, иначе unbounded.
/// </summary>
public readonly struct DataBusEventPolicy
{
    private readonly IReadOnlyDictionary<string, bool>? _burstByTypeName;

    /// <summary>Все события reliable (без burst DropOldest). Дефолт bus, если policy не передали.</summary>
    public static DataBusEventPolicy AllReliable { get; } = new(new Dictionary<string, bool>());

    /// <param name="burstByTypeName">Ключ — <see cref="Type.Name"/> типа события; значение true = burst.</param>
    public DataBusEventPolicy(IReadOnlyDictionary<string, bool> burstByTypeName)
    {
        ArgumentNullException.ThrowIfNull(burstByTypeName);
        _burstByTypeName = burstByTypeName;
    }

    public bool IsBurst(Type eventType) =>
        _burstByTypeName?.GetValueOrDefault(eventType.Name, defaultValue: false) == true;
}
