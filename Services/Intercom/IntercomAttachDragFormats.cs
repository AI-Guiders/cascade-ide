namespace CascadeIDE.Services.Intercom;

/// <summary>Drag-and-drop affordances for Intercom attach (ADR 0128 H0/H0b); wire unchanged.</summary>
public static class IntercomAttachDragFormats
{
    public const string DataFormat = "application/x-cascade-intercom-attach";

    /// <summary>Fallback via <see cref="Avalonia.Input.DataFormats.Text"/> for cross-control DnD.</summary>
    public const string TextPrefix = "cascade-intercom-attach:";

    public const string KindSelection = "selection";
    public const string KindScope = "scope";
    public const string KindProblem = "problem";

    public static string EncodeTextPayload(string inner) => TextPrefix + inner;
}
