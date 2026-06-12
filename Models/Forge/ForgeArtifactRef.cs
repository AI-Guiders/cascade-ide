namespace CascadeIDE.Models.Forge;

public enum ForgeArtifactKind
{
    Issue,
    MergeRequest,
    Repo,
}

/// <summary>Wire for <c>[FRG:repo/issues/N]</c> (CIDE ADR-0159 / FORGE-0012).</summary>
public sealed record ForgeArtifactRef(
    string Repo,
    ForgeArtifactKind Kind,
    int Number,
    string? CodeBracket = null);
