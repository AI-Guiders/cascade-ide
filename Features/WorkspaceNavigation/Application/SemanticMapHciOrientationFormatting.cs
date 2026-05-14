#nullable enable
using System.Globalization;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Одна строка для UI: компактная подпись ориентации HCI без дублирования логики индекса.</summary>
public static class SemanticMapHciOrientationFormatting
{
    /// <summary>Пустая строка — не показывать блок в UI.</summary>
    public static string ToStatusLine(SemanticMapHciOrientationSnapshot? snap)
    {
        if (snap is null)
            return "";

        if (!string.IsNullOrEmpty(snap.Error))
            return "HCI (ориентация): " + snap.Error.Trim();

        if (snap.Hits.Count == 0)
            return "";

        static string One(SemanticMapHciOrientationHit h)
        {
            var sn = (h.Snippet ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (sn.Length > 42)
                sn = sn[..39] + "…";
            return $"{h.LeafPath}:{h.Line.ToString(CultureInfo.InvariantCulture)} ({h.HitKind}) {sn}";
        }

        var head = One(snap.Hits[0]);
        if (snap.Hits.Count == 1)
            return $"HCI (ориентация) «{snap.Query}»: {head}";

        return $"HCI (ориентация) «{snap.Query}»: {head} +{snap.Hits.Count - 1}";
    }
}
