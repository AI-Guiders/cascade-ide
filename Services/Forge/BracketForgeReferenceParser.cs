using CascadeIDE.Models.Forge;

namespace CascadeIDE.Services.Forge;

/// <summary>Parse <c>[FRG:repo/issues/N]</c> and optional compound code tail (ADR-0159).</summary>
public static class BracketForgeReferenceParser
{
    public static bool TryParse(string? input, out ForgeArtifactRef reference, out string error)
    {
        reference = default!;
        error = "";

        var text = (input ?? "").Trim();
        if (text.Length == 0)
        {
            error = "Пустая bracket-ссылка.";
            return false;
        }

        if (text.StartsWith('[') && text.EndsWith(']'))
            text = text[1..^1].Trim();

        if (!text.StartsWith("FRG:", StringComparison.Ordinal))
        {
            error = "Forge bracket must start with FRG:.";
            return false;
        }

        text = text["FRG:".Length..].Trim();
        var semi = text.IndexOf(';');
        var path = semi >= 0 ? text[..semi].Trim() : text;
        var codeTail = semi >= 0 ? text[(semi + 1)..].Trim() : null;

        if (!TryParsePath(path, out var repo, out var kind, out var number, out error))
            return false;

        reference = new ForgeArtifactRef(repo, kind, number, string.IsNullOrWhiteSpace(codeTail) ? null : codeTail);
        return true;
    }

    private static bool TryParsePath(string path, out string repo, out ForgeArtifactKind kind, out int number, out string error)
    {
        repo = "";
        kind = ForgeArtifactKind.Issue;
        number = 0;
        error = "";

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 && parts[1].Equals("repos", StringComparison.OrdinalIgnoreCase))
        {
            repo = parts[0];
            kind = ForgeArtifactKind.Repo;
            number = 0;
            return repo.Length > 0;
        }

        if (parts.Length != 3)
        {
            error = "Ожидается FRG:repo/issues/N или FRG:repo/mr/N.";
            return false;
        }

        repo = parts[0];
        if (parts[1].Equals("issues", StringComparison.OrdinalIgnoreCase))
            kind = ForgeArtifactKind.Issue;
        else if (parts[1].Equals("mr", StringComparison.OrdinalIgnoreCase))
            kind = ForgeArtifactKind.MergeRequest;
        else
        {
            error = "Kind must be issues or mr.";
            return false;
        }

        if (!int.TryParse(parts[2], out number) || number <= 0)
        {
            error = "Номер issue/MR должен быть > 0.";
            return false;
        }

        return repo.Length > 0;
    }
}
