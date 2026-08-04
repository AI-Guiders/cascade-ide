namespace CascadeIDE.Services.Presentation;

/// <summary>
/// How anchors share one <c>(…)</c> TopLevel group.
/// <see cref="Split"/> = <c>+</c> (zone_separator) — simultaneous columns.
/// <see cref="OneOf"/> = <c>/</c> — XOR full zone (topology-oneof-slash-v0).
/// </summary>
public enum PresentationZoneCompose
{
    Split = 0,
    OneOf = 1,
}
