namespace CascadeIDE.Services;

/// <summary>Cockpit Command Line (ADR 0138).</summary>
public static partial class IdeCommands
{
    /// <summary>Открыть Cockpit Command Line активного Forward host (Intercom: полоса над composer). args: initial_text?:string; returns: text; example: {"initial_text":"/intercom message anchors list"}.</summary>
    public const string CockpitOpenCommandLine = "cockpit.open_command_line";
}
