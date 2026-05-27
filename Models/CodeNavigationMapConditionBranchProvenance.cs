namespace CascadeIDE.Models;

/// <summary>Маркер полярности исходящего ребра от <c>condition_step</c> (wire: <c>edge_provenance</c>).</summary>
public static class CodeNavigationMapConditionBranchProvenance
{
    public const string True = "cf_branch_true";
    public const string False = "cf_branch_false";

    public static bool TryParseDisplayPolarity(string? edgeProvenance, out bool isTrueBranch)
    {
        if (string.Equals(edgeProvenance, True, StringComparison.Ordinal))
        {
            isTrueBranch = true;
            return true;
        }

        if (string.Equals(edgeProvenance, False, StringComparison.Ordinal))
        {
            isTrueBranch = false;
            return true;
        }

        isTrueBranch = false;
        return false;
    }
}
