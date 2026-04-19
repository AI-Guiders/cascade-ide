namespace CascadeIDE.Services;

/// <summary>
/// Минимальный слой <b>Command Melody</b> (<c>c:</c>) — alias → <see cref="IdeCommands"/> id.
/// Норматив: <c>docs/intent-melody-language-v1.md</c>, ADR 0060 §11.
/// </summary>
public static class IntentMelodyAliases
{
    private static readonly Dictionary<string, string> AliasToCommandId = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gs"] = IdeCommands.GitStatus,
        ["gc"] = IdeCommands.GitCommit,
        ["gp"] = IdeCommands.GitPush,
        ["gsu"] = IdeCommands.GitSubmodule,
        ["br"] = IdeCommands.Build,
        ["bt"] = IdeCommands.RunTests,
        ["da"] = IdeCommands.DebugAttach,
        ["dr"] = IdeCommands.DebugLaunch,
        ["dc"] = IdeCommands.DebugContinue,
        // so = Solution Open — диалог .sln / .slnx (ADR 0060 §11).
        ["so"] = IdeCommands.OpenSolutionDialog,
    };

    /// <summary>Строка палитры начинается с <c>c:</c> (регистр первой буквы допускается).</summary>
    public static bool TryGetTail(string? raw, out string tailNormalized)
    {
        tailNormalized = "";
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var t = raw.TrimStart();
        if (t.Length < 2 || char.ToLowerInvariant(t[0]) != 'c' || t[1] != ':')
            return false;
        tailNormalized = t.Length > 2 ? t[2..].Trim().ToLowerInvariant() : "";
        return true;
    }

    /// <summary>Все пары (alias, command_id) по возрастанию alias.</summary>
    public static IReadOnlyList<(string Alias, string CommandId)> AllPairs() =>
        AliasToCommandId
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();

    /// <summary>Alias, чей хвост начинается с <paramref name="tailNormalized"/> (включая пустой — всё).</summary>
    public static IReadOnlyList<(string Alias, string CommandId)> FilterByTailPrefix(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
            return AllPairs();
        var list = new List<(string, string)>();
        foreach (var kv in AliasToCommandId.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (kv.Key.StartsWith(tailNormalized, StringComparison.Ordinal))
                list.Add((kv.Key, kv.Value));
        }

        return list;
    }

    public static string? TryResolveExactCommandId(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
            return null;
        return AliasToCommandId.TryGetValue(tailNormalized, out var id) ? id : null;
    }

    /// <summary>
    /// Есть ли alias длиннее <paramref name="tailNormalized"/>, который начинается с этого хвоста (например <c>gs</c> vs <c>gsu</c>).
    /// Нужно для аккорда без Enter: не исполнять точное совпадение, пока возможно продолжение.
    /// </summary>
    public static bool HasStrictLongerAliasPrefix(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
            return false;
        foreach (var kv in AliasToCommandId.Keys)
        {
            if (kv.Length > tailNormalized.Length && kv.StartsWith(tailNormalized, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
