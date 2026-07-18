namespace CascadeIDE.Models;

/// <summary>Режим доставки исходящего сообщения оператора (ADR 0116).</summary>
public static class IntercomComposerDeliveryModes
{
    public const string Normal = "normal";
    public const string Steer = "steer";
    public const string FollowUp = "follow_up";

    public static bool IsSteer(string? mode) =>
        string.Equals(mode, Steer, StringComparison.OrdinalIgnoreCase);

    public static bool IsFollowUp(string? mode) =>
        string.Equals(mode, FollowUp, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? mode) =>
        IsSteer(mode) ? Steer
        : IsFollowUp(mode) ? FollowUp
        : Normal;
}
