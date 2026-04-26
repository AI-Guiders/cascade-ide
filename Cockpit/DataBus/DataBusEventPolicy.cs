namespace CascadeIDE.Cockpit.DataBus;

/// <summary>
/// Как маршрутизировать <see cref="TEvent"/> в async-режиме <see cref="InMemoryDataBus"/>:
/// <see cref="IsBurst"/> → bounded(1) + DropOldest, иначе unbounded.
/// </summary>
public readonly struct DataBusEventPolicy
{
    private readonly IReadOnlyDictionary<string, bool>? _burstByTypeName;

    /// <param name="burstByTypeName">Ключ — <see cref="Type.Name"/> типа события; значение true = burst.</param>
    public DataBusEventPolicy(IReadOnlyDictionary<string, bool> burstByTypeName)
    {
        ArgumentNullException.ThrowIfNull(burstByTypeName);
        _burstByTypeName = burstByTypeName;
    }

    public bool IsBurst(Type eventType) =>
        _burstByTypeName?.GetValueOrDefault(eventType.Name, defaultValue: false) == true;
}
