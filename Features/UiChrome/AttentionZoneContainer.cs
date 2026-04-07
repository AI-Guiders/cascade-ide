using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Контейнер <strong>пространственного якоря</strong> (лобовое / PFD / MFD): один канонический <see cref="AttentionZone"/>,
/// без путаницы с именами вроде PrimaryFlightDisplay. Канал EICAS (оповещения) по ADR — отдельный контур,
/// не «четвёртая колонка»; UI — <c>EicasAlertsBarView</c> / <c>eicas_alerts_bar</c>, не обязательно этот контроль.
/// </summary>
public sealed class AttentionZoneContainer : ContentControl
{
    public static readonly StyledProperty<AttentionZone> ZoneProperty =
        AvaloniaProperty.Register<AttentionZoneContainer, AttentionZone>(nameof(Zone));

    static AttentionZoneContainer()
    {
        ZoneProperty.Changed.AddClassHandler<AttentionZoneContainer>((c, _) => c.SyncZoneClasses());
    }

    /// <summary>Начальная зона <see cref="AttentionZone.Frontal"/> не даёт <c>ZoneProperty.Changed</c>; без синхронизации классы пусты до логического дерева.</summary>
    public AttentionZoneContainer() => SyncZoneClasses();

    public AttentionZone Zone
    {
        get => GetValue(ZoneProperty);
        set => SetValue(ZoneProperty, value);
    }

    /// <summary>Каноническая строка для TOML/MCP (например <c>pfd</c>).</summary>
    public string CanonicalZoneId => Zone.ToCanonicalId();

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        SyncZoneClasses();
    }

    private void SyncZoneClasses()
    {
        foreach (var token in ZoneClassTokens)
            Classes.Remove(token);

        // Три якоря — общий маркер attention-zone + суффикс. EICAS/HUD по ADR не «зоны-якоря».
        switch (Zone)
        {
            case AttentionZone.Eicas:
                Classes.Add("attention-channel-eicas");
                return;
            case AttentionZone.Hud:
                Classes.Add("attention-layer-hud");
                return;
            case AttentionZone.Frontal:
                Classes.Add("attention-zone");
                Classes.Add("attention-zone-frontal");
                return;
            case AttentionZone.Pfd:
                Classes.Add("attention-zone");
                Classes.Add("attention-zone-pfd");
                return;
            case AttentionZone.Mfd:
                Classes.Add("attention-zone");
                Classes.Add("attention-zone-mfd");
                return;
            default:
                Classes.Add("attention-zone");
                Classes.Add("attention-zone-frontal");
                return;
        }
    }

    private static readonly string[] ZoneClassTokens =
    [
        "attention-zone",
        "attention-zone-frontal",
        "attention-zone-pfd",
        "attention-zone-mfd",
        "attention-channel-eicas",
        "attention-layer-hud",
    ];
}
