namespace CascadeIDE.Features.Editor.Application.Presentation;

/// <summary>
/// Текст полосы **HUD banner** над редактором (ADR 0021 §9, 0085 — не путать с inline Editor HUD).
/// Слой презентации: без DAL, без C#-сервисов; только форматирование сегментов.
/// </summary>
public static class EditorHudBannerTextComposer
{
    public static string? FormatDiagnosticSummary(int errorCount, int warningCount)
    {
        if (errorCount > 0 && warningCount > 0)
            return $"{errorCount} ошибок, {warningCount} предупреждений";
        if (errorCount > 0)
            return errorCount == 1 ? "1 ошибка" : $"{errorCount} ошибок";
        if (warningCount > 0)
            return warningCount == 1 ? "1 предупреждение" : $"{warningCount} предупреждений";
        return null;
    }

    public static string? FormatReferenceOccurrenceSummary(int count)
    {
        if (count <= 0)
            return null;
        return count == 1
            ? "1 вхождение в файле"
            : $"{count} вхождений в файле";
    }

    public static string? Combine(string? diagnosticPart, string? referencePart)
    {
        if (diagnosticPart is null && referencePart is null)
            return null;
        if (diagnosticPart is not null && referencePart is not null)
            return $"{diagnosticPart} · {referencePart}";
        return diagnosticPart ?? referencePart;
    }
}
