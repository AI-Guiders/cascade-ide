namespace CascadeIDE.Models;

/// <summary>Настройки Intercom. TOML: <c>[intercom]</c> (ADR 0130 фаза 3).</summary>
public sealed class IntercomSettings
{
    /// <summary>
    /// Дефолт навигации по клику на attach-chip: <c>reveal</c> (transient highlight) или <c>select</c> (selection в редакторе).
    /// Shift+клик всегда select. MCP с явным <c>select</c> переопределяет дефолт.
    /// </summary>
    public string AttachmentNavigate { get; set; } = "reveal";

    public bool DefaultAttachmentNavigateSelects() =>
        string.Equals(AttachmentNavigate, "select", StringComparison.OrdinalIgnoreCase);
}
