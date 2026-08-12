#nullable enable
using System.Text;
using CascadeIDE.Cockpit.Channels.Eicas;

namespace CascadeIDE.Services;

/// <summary>
/// Afferent desk pulse for MAF host (citizen wire peel #12).
/// Transfers Dark Cockpit scan: SoftInstrument-visible board → <c>@frame desk</c> grammar
/// compatible with CDP <c>CitizenWire.PackDesk</c> — no second SSOT.
/// </summary>
internal static class CitizenDeskAfferent
{
    internal const int MaxPackedChars = 900;

    /// <summary>Pack SoftInstrument + EICAS salience into wire pulse. Empty board → null.</summary>
    internal static string? TryPackFromHabitat(
        IEnumerable<string>? chromeVisibleLines,
        IEnumerable<EicasMessage>? eicasMessages,
        string? saDeskHint,
        string? planHint,
        string? ideHealthPeer)
    {
        var boardLines = new List<string>();
        if (chromeVisibleLines is not null)
        {
            foreach (var line in chromeVisibleLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    boardLines.Add(line.Trim());
            }
        }

        if (eicasMessages is not null)
        {
            var n = 0;
            foreach (var msg in eicasMessages)
            {
                var text = msg.Text?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                boardLines.Add($"{msg.Severity}: {text}");
                if (++n >= 3)
                    break;
            }
        }

        return TryPack(
            boardLines,
            sa: FormatSa(eicasMessages, saDeskHint),
            tm: planHint,
            peer: string.IsNullOrWhiteSpace(ideHealthPeer) ? null : ideHealthPeer.Trim());
    }

    /// <summary>Pack board lines + sa/tm into wire pulse. Empty board → null (silent Dark Cockpit).</summary>
    internal static string? TryPack(
        IEnumerable<string>? boardLines,
        string? sa = null,
        string? tm = null,
        string? peer = null,
        string cost = "A",
        string version = "v0")
    {
        var board = FormatBoard(boardLines);
        if (string.IsNullOrWhiteSpace(board))
            return null;

        var sb = new StringBuilder();
        sb.Append("@frame desk ").Append(version).Append('\n');
        AppendField(sb, "board", board);
        AppendField(sb, "sa", string.IsNullOrWhiteSpace(sa) ? "clear" : sa.Trim());
        if (!string.IsNullOrWhiteSpace(tm))
            AppendField(sb, "tm", tm.Trim());
        if (!string.IsNullOrWhiteSpace(peer))
            AppendField(sb, "peer", peer.Trim());
        AppendField(sb, "cost", string.IsNullOrWhiteSpace(cost) ? "A" : cost.Trim());

        var packed = sb.ToString();
        if (packed.Length <= MaxPackedChars)
            return packed;
        return packed[..MaxPackedChars].TrimEnd() + "\n… [desk pulse truncated]\n";
    }

    /// <summary>Prepend pulse ahead of hot/telemetry/file context (afferent first).</summary>
    internal static string? MergeIntoMinimized(string? deskPulse, string? existingMinimized)
    {
        var pulse = deskPulse?.Trim();
        var rest = existingMinimized?.Trim();
        if (string.IsNullOrWhiteSpace(pulse))
            return string.IsNullOrWhiteSpace(rest) ? null : rest;
        if (string.IsNullOrWhiteSpace(rest))
            return pulse;
        return pulse + "\n\n---\n\n" + rest;
    }

    static string FormatSa(IEnumerable<EicasMessage>? eicasMessages, string? saDeskHint)
    {
        if (eicasMessages is not null)
        {
            foreach (var top in eicasMessages)
            {
                var t = top.Text?.Trim();
                return string.IsNullOrWhiteSpace(t)
                    ? $"eicas · {top.Severity}"
                    : $"eicas · {top.Severity} · {t}";
            }
        }

        if (!string.IsNullOrWhiteSpace(saDeskHint))
            return saDeskHint.Trim();

        return "clear";
    }

    static string FormatBoard(IEnumerable<string>? boardLines)
    {
        if (boardLines is null)
            return "";

        var parts = new List<string>();
        foreach (var raw in boardLines)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            var line = raw.Trim();
            if (line.Length > 160)
                line = line[..160].TrimEnd() + "…";
            parts.Add(line);
            if (parts.Count >= 8)
                break;
        }

        return string.Join(" | ", parts);
    }

    static void AppendField(StringBuilder sb, string key, string value) =>
        sb.Append(key).Append(" | ").Append(value.Trim()).Append('\n');
}
