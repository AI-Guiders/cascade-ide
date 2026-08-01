#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>Glass indication severity — Dark Cockpit: quiet by default; color only on deviation.</summary>
public enum GlassChipLevel
{
    Quiet = 0,
    Caution = 1,
    Warn = 2,
    Fail = 3,
}
