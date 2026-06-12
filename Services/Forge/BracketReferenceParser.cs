using CascadeIDE.Models.Forge;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Services.Forge;

public enum BracketReferenceKind
{
    Code,
    Forge,
}

/// <summary>Facade: <c>FRG:</c> vs code axes (ADR-0159 §7).</summary>
public static class BracketReferenceParser
{
    public static bool TryParse(string? input, out BracketReferenceKind kind, out ForgeArtifactRef forge, out BracketCodeReference code, out string error)
    {
        forge = default!;
        code = default;
        error = "";

        var raw = (input ?? "").Trim();
        if (raw.Length == 0)
        {
            kind = BracketReferenceKind.Code;
            error = "Пустая bracket-ссылка.";
            return false;
        }

        var inner = raw;
        if (inner.StartsWith('[') && inner.EndsWith(']') && inner.Length >= 2)
            inner = inner[1..^1].Trim();

        if (inner.StartsWith("FRG:", StringComparison.Ordinal))
        {
            kind = BracketReferenceKind.Forge;
            var bracket = raw.StartsWith('[') ? raw : $"[{inner}]";
            return BracketForgeReferenceParser.TryParse(bracket, out forge, out error);
        }

        kind = BracketReferenceKind.Code;
        return BracketCodeReferenceParser.TryParse(raw, out code, out error);
    }
}
