#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>Human SoftOrgan Face — situations → steps (ADR 0014), not markdown wall / not jargon chips.</summary>
public static class SoftOrganFaceHandbook
{
    public static string MfdPageFor(string organId) =>
        (organId ?? "").Trim().ToLowerInvariant() switch
        {
            "qrh" => "QRH",
            "ecl" => "ECL",
            "alert" or "eicas" or "sa" => "Alert",
            "here" or "herenext" or "next" => "HereNext",
            _ => "QRH",
        };

    public static bool IsSoftOrganGlancePage(string? page) =>
        page is "QRH" or "ECL" or "Alert" or "HereNext";

    public static string OrganIdFromMfdPage(string? page) =>
        (page ?? "").Trim() switch
        {
            "QRH" => "qrh",
            "ECL" => "ecl",
            "Alert" => "alert",
            "HereNext" => "here",
            _ => "qrh",
        };

    public static IReadOnlyList<OperatorSituation> SituationsFor(string organId, string? filter = null) =>
        OperatorSituationCatalog.ForFamily(OrganIdFromMfdPage(MfdPageFor(organId)), filter);

    public static IReadOnlyList<GlassGlanceChip> ChipsFor(string organId, string? filter = null) =>
        SituationsFor(organId, filter).Select(OperatorSituationCatalog.ToChip).ToList();
}
