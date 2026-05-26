namespace IntercomWire;

/// <summary>Канон <c>event_kinds</c> с <c>sync_default: true</c> (intercom-wire/schemas/v1/event-kinds.json).</summary>
public static class IntercomWireTransportEventKinds
{
    public static IReadOnlySet<string> SyncDefault { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "message_added",
        "message_completed",
        "message_edited",
        "thread_forked",
        "message_range_related",
    };
}
