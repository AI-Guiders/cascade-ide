namespace CascadeIDE.Models;

/// <summary>Лобовой якорь внимания (ADR 0120): редактор или Intercom в Forward.</summary>
public enum PrimaryWorkSurfaceKind
{
    Editor = 0,
    Intercom = 1,
}

public static class PrimaryWorkSurfaceKindExtensions
{
    public const string EditorValue = "editor";
    public const string IntercomValue = "intercom";

    public static string ToTomlValue(this PrimaryWorkSurfaceKind kind) =>
        kind == PrimaryWorkSurfaceKind.Intercom ? IntercomValue : EditorValue;

    public static PrimaryWorkSurfaceKind ParseTomlValue(string? raw) =>
        string.Equals(raw?.Trim(), IntercomValue, StringComparison.OrdinalIgnoreCase)
            ? PrimaryWorkSurfaceKind.Intercom
            : PrimaryWorkSurfaceKind.Editor;
}
