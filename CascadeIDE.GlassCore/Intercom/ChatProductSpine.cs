namespace CascadeIDE.Features.Chat;

/// <summary>Сквозная линия продукта (spine) сессии — ортогональна тредам ([ADR 0096](../../docs/adr/0096-intercom-topic-card-summary-and-product-spine.md)).</summary>
public sealed record ChatProductSpine(
    string LineTitle,
    string CurrentFocus,
    IReadOnlyList<string> Milestones,
    bool IncludeInAgentContext)
{
    public static ChatProductSpine Empty { get; } = new("", "", [], false);

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(LineTitle)
        || !string.IsNullOrWhiteSpace(CurrentFocus)
        || Milestones.Count > 0;

    public bool HasFocusOrMilestones =>
        !string.IsNullOrWhiteSpace(CurrentFocus) || Milestones.Count > 0;

    public string FormatCardBody()
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(CurrentFocus))
            lines.Add(CurrentFocus.Trim());
        foreach (var milestone in Milestones)
        {
            if (string.IsNullOrWhiteSpace(milestone))
                continue;
            lines.Add("• " + milestone.Trim());
        }

        return lines.Count == 0 ? "Задай фокус линии и вехи над чатом." : string.Join('\n', lines);
    }

    public string? BuildAgentContextPrefix()
    {
        if (!IncludeInAgentContext || !HasContent)
            return null;

        var lines = new List<string> { "[Продуктовая линия — сжатый срез]" };
        if (!string.IsNullOrWhiteSpace(LineTitle))
            lines.Add($"Линия: {LineTitle.Trim()}");
        if (!string.IsNullOrWhiteSpace(CurrentFocus))
            lines.Add($"Фокус: {CurrentFocus.Trim()}");
        var bullets = Milestones
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Take(3)
            .Select(m => $"• {m.Trim()}")
            .ToList();
        if (bullets.Count > 0)
            lines.AddRange(bullets);
        lines.Add("---");
        return string.Join(Environment.NewLine, lines);
    }

    public static IReadOnlyList<string> ParseMilestonesText(string? text) =>
        (text ?? "")
            .Replace("\r", "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 0)
            .Take(8)
            .ToList();

    public static string JoinMilestonesText(IReadOnlyList<string> milestones) =>
        string.Join(Environment.NewLine, milestones);
}
