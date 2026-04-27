using System.Text.RegularExpressions;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>Debug-hints редактора: EOL-подсказки по текущей остановке DAP и top-level переменным.</summary>
public partial class MainWindowViewModel
{
    private static readonly Regex s_assignmentRegex = new(
        @"^\s*(?:(?:var|[A-Za-z_][\w<>,\[\]\?\s]*)\s+)?([A-Za-z_]\w*)\s*=",
        RegexOptions.Compiled);

    private static readonly Regex s_conditionRegex = new(
        @"^\s*(if|while|switch)\s*\((?<expr>.*)\)",
        RegexOptions.Compiled);

    private static readonly Regex s_identifierRegex = new(
        @"\b[_A-Za-z]\w*\b",
        RegexOptions.Compiled);

    /// <summary>Собрать EOL debug hints для файла: в v1 только текущая исполняемая строка.</summary>
    public IReadOnlyList<EditorDebugHintStrip> GetEditorDebugHintsForFile(string filePath, string sourceText)
    {
        var opts = _settings.Editor.DebugHints;
        if (!opts.Enabled)
            return [];

        var snapshot = DapDebug.GetSnapshot();
        if (!snapshot.IsExecutionStopped || string.IsNullOrWhiteSpace(snapshot.StoppedFile))
            return [];

        var currentLine = GetDebugCurrentLineForFile(filePath);
        if (currentLine <= 0)
            return [];

        var lines = sourceText.Split('\n');
        if (currentLine > lines.Length)
            return [];
        var lineText = lines[currentLine - 1].Trim();
        if (string.IsNullOrWhiteSpace(lineText))
            return [];

        var variables = snapshot.VariableRootScopes
            .SelectMany(static s => s.Roots)
            .Where(static r => !string.IsNullOrWhiteSpace(r.Name))
            .GroupBy(static r => r.Name, StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.First().Value, StringComparer.Ordinal);

        string? label = null;
        if (opts.ShowAssignments)
            label = TryBuildAssignmentLabel(lineText, variables);
        if (label is null && opts.ShowConditions)
            label = TryBuildConditionLabel(lineText, variables);
        if (label is null)
            label = TryBuildIdentifierSnapshotLabel(lineText, variables);

        if (string.IsNullOrWhiteSpace(label))
            return [];

        if (InlayHintTrace.IsDebug)
            InlayHintTrace.LogDebug($"DebugHint.Model line={currentLine} label={label}");

        return [new EditorDebugHintStrip(currentLine, label)];
    }

    private static string? TryBuildAssignmentLabel(string lineText, IReadOnlyDictionary<string, string> vars)
    {
        var m = s_assignmentRegex.Match(lineText);
        if (!m.Success)
            return null;
        var name = m.Groups[1].Value;
        if (!vars.TryGetValue(name, out var value))
            return null;
        return $"{name} = {value}";
    }

    private static string? TryBuildConditionLabel(string lineText, IReadOnlyDictionary<string, string> vars)
    {
        var m = s_conditionRegex.Match(lineText);
        if (!m.Success)
            return null;
        var expr = m.Groups["expr"].Value;
        var values = ExtractVariableValues(expr, vars);
        if (values.Count == 0)
            return null;
        return "=> " + string.Join(", ", values);
    }

    private static string? TryBuildIdentifierSnapshotLabel(string lineText, IReadOnlyDictionary<string, string> vars)
    {
        var values = ExtractVariableValues(lineText, vars);
        if (values.Count == 0)
            return null;
        return "=> " + string.Join(", ", values);
    }

    private static List<string> ExtractVariableValues(string source, IReadOnlyDictionary<string, string> vars)
    {
        var values = new List<string>(4);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match id in s_identifierRegex.Matches(source))
        {
            var name = id.Value;
            if (!seen.Add(name))
                continue;
            if (!vars.TryGetValue(name, out var value))
                continue;
            values.Add($"{name}={value}");
            if (values.Count >= 4)
                break;
        }

        return values;
    }
}
