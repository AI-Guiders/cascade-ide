#nullable enable
using CascadeIDE.Features.Cdp;
using CascadeIDE.Intercom;
using Cdp.IntercomJournal;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Virtual History cold store — shared <c>intercom.witdb</c> with cdp-mcp.
/// Human scroll survives restart; PF uses <c>cdp_intercom op=history</c>.
/// </summary>
internal static class GlassIntercomJournal
{
    public const string FileName = IntercomJournalStore.FileName;
    public const string LegacyJsonlFileName = IntercomJournalStore.LegacyJsonlFileName;

    public static string JournalPath => IntercomJournalStore.DbPath(CdpHabitatPaths.StateRoot);

    public sealed record Entry(
        string Id,
        string FromSeat,
        string ToSeat,
        string Body,
        string Origin,
        DateTimeOffset StampedUtc,
        string RoleLabel,
        string WhenLabel,
        IReadOnlyList<GlassAttachChip> Chips,
        string Channel);

    public static void Append(
        string id,
        string fromSeat,
        string toSeat,
        string body,
        string origin,
        DateTimeOffset stampedUtc,
        string? name = null,
        string? kind = null,
        string? channel = null)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(body))
            return;

        var (resolvedName, resolvedKind) = LatchPaint.ResolveIntercomIdentity(fromSeat, origin, name, kind);
        var channelCode = GlassIntercomChannel.Code(GlassIntercomChannel.Parse(channel));
        _ = IntercomJournalStore.TryAppend(
            CdpHabitatPaths.StateRoot,
            new IntercomJournalRow
            {
                Id = id.Trim(),
                FromSeat = fromSeat,
                ToSeat = toSeat,
                Body = body,
                Origin = origin,
                Name = resolvedName,
                Kind = resolvedKind,
                Channel = channelCode,
                StampedUtc = stampedUtc,
                Acked = false
            });
    }

    public static IReadOnlyList<Entry> LoadTail(int limit = 40)
    {
        if (limit < 1) limit = 1;
        if (limit > 500) limit = 500;

        var rows = IntercomJournalStore.LoadTail(CdpHabitatPaths.StateRoot, limit);
        var list = new List<Entry>(rows.Count);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Body))
                continue;
            var when = row.StampedUtc.ToLocalTime().ToString("HH:mm");
            var (resolvedName, resolvedKind) = LatchPaint.ResolveIntercomIdentity(
                row.FromSeat, row.Origin, row.Name, row.Kind);
            var role = LatchPaint.FormatIntercomRole(row.FromSeat, row.ToSeat, resolvedName, resolvedKind);
            var channel = row.Channel ?? GlassIntercomChannel.Code(GlassIntercomChannel.DefaultKind);
            var chips = GlassAttachChipPeel.Peel(row.Body);
            list.Add(new Entry(
                row.Id,
                row.FromSeat,
                row.ToSeat,
                row.Body.Replace("\r\n", "\n"),
                row.Origin,
                row.StampedUtc,
                role,
                when,
                chips,
                channel));
        }

        return list;
    }
}
