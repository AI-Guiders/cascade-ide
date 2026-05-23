namespace CascadeIDE.Models;

/// <summary>Настройки Intercom. TOML: <c>[intercom.*]</c> (ADR 0130).</summary>
public sealed class IntercomSettings
{
    /// <summary>
    /// Плотность ленты и composer: <c>comfortable</c> (prose_pt, MFD-отступы) или <c>compact</c> (prose_pt_forward + SkiaChatDensity).
    /// TOML: <c>[intercom] feed_metrics</c>.
    /// </summary>
    public string FeedMetrics { get; set; } = IntercomFeedMetricsModes.Compact;

    /// <summary>
    /// Глиф валидации slash в CCL/composer: <c>left</c>, <c>right</c> (по умолчанию), <c>highlight_only</c>.
    /// TOML: <c>[intercom] tci_validation_icon</c>.
    /// </summary>
    public string TciValidationIcon { get; set; } = TciValidationIconModes.Right;

    /// <summary>Вложения в ленте. TOML: <c>[intercom.attachments.*]</c>.</summary>
    public IntercomAttachmentsSettings Attachments { get; set; } = new();

    public bool UseComfortableFeedMetrics() => IntercomFeedMetricsModes.IsComfortable(FeedMetrics);
}

/// <summary>TOML: секция-родитель <c>[intercom.attachments]</c> (поля — во вложенных таблицах).</summary>
public sealed class IntercomAttachmentsSettings
{
    /// <summary>Code-якоря (F:/M:/L:, chip, reveal/select). TOML: <c>[intercom.attachments.code]</c>.</summary>
    public IntercomAttachmentsCodeSettings Code { get; set; } = new();
}

/// <summary>Навигация и загрузка решения для code-attach. TOML: <c>[intercom.attachments.code]</c>.</summary>
public sealed class IntercomAttachmentsCodeSettings
{
    /// <summary>
    /// Клик по attach-chip: <c>reveal</c> (transient highlight) или <c>select</c> (selection в редакторе).
    /// Shift+клик всегда select. MCP с явным <c>select</c> переопределяет дефолт.
    /// </summary>
    public string Navigate { get; set; } = "reveal";

    /// <summary>
    /// Перед reveal: <c>when_needed</c> — .sln от файла, если не совпадает с открытым;
    /// <c>never</c> — только open/reveal файла.
    /// </summary>
    public string RevealLoadSolution { get; set; } = IntercomAttachmentsCodeRevealLoadSolutionModes.WhenNeeded;

    public bool DefaultNavigateSelects() =>
        string.Equals(Navigate, "select", StringComparison.OrdinalIgnoreCase);

    public bool ShouldLoadSolutionBeforeReveal() =>
        !string.Equals(
            RevealLoadSolution?.Trim(),
            IntercomAttachmentsCodeRevealLoadSolutionModes.Never,
            StringComparison.OrdinalIgnoreCase);
}

/// <summary>Значения <see cref="IntercomAttachmentsCodeSettings.RevealLoadSolution"/>.</summary>
public static class IntercomAttachmentsCodeRevealLoadSolutionModes
{
    public const string WhenNeeded = "when_needed";
    public const string Never = "never";
}
