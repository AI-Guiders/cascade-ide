namespace CascadeIDE.Cockpit.DataBus;

/// <summary>
/// Единая проекция LSP stdio (C# / Markdown) для шины и для ER: процесс в handshake/жив, и прикреплённый инстанс хоста (страт C, ADR 0099).
/// Заполнять из одной функции вместе с <c>Publish(…)</c> в DataBus; не дублировать <c>host.IsActive</c> вне этого типа.
/// </summary>
public readonly record struct IdeHostStateChanged(
    bool CSharpLspProcessActive,
    bool MarkdownLspProcessActive,
    bool CSharpLspHostPresent,
    bool MarkdownLspHostPresent);
