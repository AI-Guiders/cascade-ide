#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Re-export platform SlashArgTailKind (GUIDERS-ADR-0003 P5).</summary>
public enum SlashArgTailKind
{
    None = AIGuiders.Platform.CommandPlane.SlashArgTailKind.None,
    Optional = AIGuiders.Platform.CommandPlane.SlashArgTailKind.Optional,
    Required = AIGuiders.Platform.CommandPlane.SlashArgTailKind.Required,
}

