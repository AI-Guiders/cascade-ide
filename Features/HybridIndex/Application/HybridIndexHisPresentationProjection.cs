using System.Globalization;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>Статические строки лампы/сводки HIS (MFD) по событию DataBus без VM.</summary>
[ComputingUnit("hybrid-index-his")]
public static class HybridIndexHisPresentationProjection
{
    public static string LampText(HybridIndexStateChanged? last) =>
        last is null
            ? "NO DATA"
            : string.IsNullOrWhiteSpace(last.LastError)
                ? "OK"
                : "CAUTION";

    public static string StateShort(HybridIndexStateChanged? last) =>
        last is null
            ? "—"
            : string.IsNullOrWhiteSpace(last.LastError)
                ? "IDLE"
                : "ERROR";

    /// <param name="lastErrorBannerText">Текст ошибки для баннера HIS или «—», если пусто.</param>
    public static string SecondMessageLine(string lastErrorBannerText) =>
        string.IsNullOrWhiteSpace(lastErrorBannerText) || lastErrorBannerText == "—"
            ? "NO FAILURES"
            : lastErrorBannerText;

    /// <summary>Шкала 0..1 по числу документов (верхняя граница — выбор UX).</summary>
    public static double DocsGauge01(int documentCount, double maxDocs = 3000.0)
    {
        if (documentCount <= 0 || maxDocs <= 0)
            return 0;
        return Math.Clamp(documentCount / maxDocs, 0, 1);
    }

    public static double FreshnessTotalMinutes(string? indexedAtIso, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(indexedAtIso))
            return 0;
        if (!DateTimeOffset.TryParse(indexedAtIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ts))
            return 0;
        var age = utcNow - ts;
        if (age < TimeSpan.Zero)
            age = TimeSpan.Zero;
        return age.TotalMinutes;
    }

    public static string FreshnessMinutesRoundedText(double totalMinutes)
    {
        if (totalMinutes <= 0.5)
            return "0";
        if (totalMinutes >= 10_000)
            return "9999";
        return Math.Floor(totalMinutes).ToString(CultureInfo.InvariantCulture);
    }

    public static string FreshnessEcamText(double totalMinutes)
    {
        if (totalMinutes <= 0.5)
            return "0m";

        if (totalMinutes < 60)
            return $"{Math.Floor(totalMinutes).ToString(CultureInfo.InvariantCulture)}m";

        var h = totalMinutes / 60.0;
        if (h < 24)
            return $"{Math.Floor(h).ToString(CultureInfo.InvariantCulture)}h";

        var d = h / 24.0;
        if (d >= 100)
            return "99d";
        return $"{Math.Floor(d).ToString(CultureInfo.InvariantCulture)}d";
    }

    public static string IndexedAtOrDash(string? indexedAtIso) =>
        string.IsNullOrWhiteSpace(indexedAtIso) ? "—" : indexedAtIso;

    public static string FreshnessColonLine(string? indexedAtIso, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(indexedAtIso))
            return "freshness: —";
        if (!DateTimeOffset.TryParse(indexedAtIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var ts))
            return "freshness: ?";
        var age = utcNow - ts;
        if (age < TimeSpan.Zero)
            age = TimeSpan.Zero;
        if (age.TotalHours >= 24)
            return $"freshness: {Math.Floor(age.TotalDays)}d";
        if (age.TotalMinutes >= 60)
            return $"freshness: {Math.Floor(age.TotalHours)}h";
        return $"freshness: {Math.Floor(age.TotalMinutes)}m";
    }

    public static string OptionalFieldOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    public static string LastErrorOrDash(string? lastError) =>
        string.IsNullOrWhiteSpace(lastError) ? "—" : lastError!;

    public static AnnunciatorLampItem LampItem(HybridIndexStateChanged? last)
    {
        if (last is null)
        {
            return new AnnunciatorLampItem(
                Id: "hci",
                Title: "HCI",
                Detail: "No data yet.",
                Level: AnnunciatorLampLevel.Advisory,
                LampShortLabel: "HCI");
        }

        var level = string.IsNullOrWhiteSpace(last.LastError)
            ? AnnunciatorLampLevel.Ok
            : AnnunciatorLampLevel.Caution;

        var detail = string.IsNullOrWhiteSpace(last.LastError)
            ? "OK"
            : last.LastError!;

        return new AnnunciatorLampItem(
            Id: "hci",
            Title: "HCI",
            Detail: detail,
            Level: level,
            LampShortLabel: "HCI");
    }
}
