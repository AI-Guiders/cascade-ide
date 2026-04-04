using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// Run from repo/cascade-ide folder:
//   dotnet run --project tools/CascadeIDE.ProtocolDocGen -- [--cs-only|--md-only|--lint] <optional: path-to-cascade-ide>

const int ExitSuccess = 0;
const int ExitUsageOrMissingInput = 2;
const int ExitLintFailed = 3;

static int PrintUsage()
{
    Console.Error.WriteLine("CascadeIDE.ProtocolDocGen");
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  dotnet run --project tools/CascadeIDE.ProtocolDocGen -- [--cs-only|--md-only|--lint] [<path-to-cascade-ide>]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Options:");
    Console.Error.WriteLine("  --cs-only   Generate only *.g.cs files (no MCP-PROTOCOL.md update).");
    Console.Error.WriteLine("  --md-only   Update MCP-PROTOCOL.md only (no *.g.cs generation).");
    Console.Error.WriteLine("  --lint      Validate IdeCommands.xml doc contract only (build gate).");
    Console.Error.WriteLine("  -h|--help   Show help.");
    return ExitUsageOrMissingInput;
}

if (args.Any(a => string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)))
    return PrintUsage();

var csOnly = args.Any(a => string.Equals(a, "--cs-only", StringComparison.OrdinalIgnoreCase));
var mdOnly = args.Any(a => string.Equals(a, "--md-only", StringComparison.OrdinalIgnoreCase));
var lintOnly = args.Any(a => string.Equals(a, "--lint", StringComparison.OrdinalIgnoreCase));
if (csOnly && mdOnly)
{
    Console.Error.WriteLine("Specify at most one of --cs-only / --md-only.");
    return PrintUsage();
}
if (lintOnly && (csOnly || mdOnly))
{
    Console.Error.WriteLine("Specify --lint alone (do not combine with --cs-only/--md-only).");
    return PrintUsage();
}

var positional = args.Where(a => !a.StartsWith("-", StringComparison.Ordinal)).ToArray();
if (positional.Length > 1)
{
    Console.Error.WriteLine($"Too many positional arguments: {string.Join(" ", positional.Select(p => $"'{p}'"))}");
    return PrintUsage();
}
var rootArg = positional.FirstOrDefault();
var root = !string.IsNullOrWhiteSpace(rootArg) ? Path.GetFullPath(rootArg) : Directory.GetCurrentDirectory();
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"Missing directory: {root}");
    return PrintUsage();
}

var ideCommandsPath = Path.Combine(root, "Services", "IdeCommands.cs");
var protocolPath = Path.Combine(root, "docs", "MCP-PROTOCOL.md");
var generatedCsPath = Path.Combine(root, "Services", "Generated", "IdeCommandsDoc.g.cs");
var generatedArgsPath = Path.Combine(root, "Services", "Generated", "IdeCommandsArgsGenerated.g.cs");
var generatedContractPath = Path.Combine(root, "Services", "Generated", "IdeCommandsContract.g.cs");
var generatedExecutorPath = Path.Combine(root, "ViewModels", "Generated", "IdeMcpCommandExecutor.Generated.g.cs");

if (!File.Exists(ideCommandsPath))
{
    Console.Error.WriteLine($"Missing file: {ideCommandsPath}");
    return PrintUsage();
}
if (!File.Exists(protocolPath))
{
    Console.Error.WriteLine($"Missing file: {protocolPath}");
    return PrintUsage();
}

var items = IdeCommandsParser.Parse(File.ReadAllText(ideCommandsPath, Encoding.UTF8));

if (lintOnly)
{
    var errors = IdeCommandsLint.Run(items);
    if (errors.Count > 0)
    {
        foreach (var e in errors)
            Console.Error.WriteLine(e);
        return ExitLintFailed;
    }
    Console.WriteLine("OK");
    return ExitSuccess;
}

if (!mdOnly)
{
    Directory.CreateDirectory(Path.GetDirectoryName(generatedCsPath)!);
    File.WriteAllText(generatedCsPath, IdeCommandsDocEmitter.Emit(items), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    File.WriteAllText(generatedArgsPath, IdeCommandsArgsEmitter.Emit(items), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    File.WriteAllText(generatedContractPath, IdeCommandsContractEmitter.Emit(items), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Directory.CreateDirectory(Path.GetDirectoryName(generatedExecutorPath)!);
    File.WriteAllText(generatedExecutorPath, IdeMcpCommandExecutorEmitter.Emit(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

if (!csOnly)
{
    var protocol = File.ReadAllText(protocolPath, Encoding.UTF8);
    var updated = MarkerReplace.ReplaceSection(
        protocol,
        startMarker: "<!-- GENERATED:IdeCommands START -->",
        endMarker: "<!-- GENERATED:IdeCommands END -->",
        newContent: ProtocolMdEmitter.EmitIdeCommandsTable(items));
    File.WriteAllText(protocolPath, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

Console.WriteLine("OK");
return ExitSuccess;

internal sealed record IdeCommandDoc(string FieldName, string CommandId, string? Summary, string Category);

internal readonly record struct IdeCommandArg(string Name, string JsonType, bool Required, bool IsArray, string? ItemJsonType);

internal static class IdeCommandsParser
{
    private static readonly Regex ConstString = new(@"public\s+const\s+string\s+(?<name>\w+)\s*=\s*""(?<value>[^""]+)""\s*;", RegexOptions.Compiled);

    public static IReadOnlyList<IdeCommandDoc> Parse(string text)
    {
        var lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
        var docs = new List<IdeCommandDoc>();

        var pendingSummary = new StringBuilder();
        var inSummary = false;
        var category = "Core";

        for (var i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var line = raw.Trim();

            if (line.Contains("class IdeCommands", StringComparison.Ordinal))
            {
                // Discard type-level XML doc (we only want per-command summaries).
                pendingSummary.Clear();
                inSummary = false;
                continue;
            }

            // Category separators in IdeCommands.cs:
            if (line.StartsWith("// ———", StringComparison.Ordinal))
            {
                category = line.TrimStart('/').Trim().Trim('—', ' ', '\t');
                continue;
            }

            if (line.StartsWith("///", StringComparison.Ordinal))
            {
                var body = line[3..].Trim();
                if (body.StartsWith("<summary>", StringComparison.Ordinal))
                {
                    inSummary = true;
                    body = body["<summary>".Length..];
                }

                if (inSummary)
                {
                    var endIdx = body.IndexOf("</summary>", StringComparison.Ordinal);
                    if (endIdx >= 0)
                    {
                        pendingSummary.AppendLine(body[..endIdx]);
                        inSummary = false;
                    }
                    else
                    {
                        pendingSummary.AppendLine(body);
                    }
                }

                continue;
            }

            var m = ConstString.Match(line);
            if (!m.Success)
                continue;

            var fieldName = m.Groups["name"].Value;
            var commandId = m.Groups["value"].Value;
            var summary = NormalizeSummary(pendingSummary.ToString());
            pendingSummary.Clear();
            inSummary = false;

            docs.Add(new IdeCommandDoc(fieldName, commandId, summary, category));
        }

        return docs;
    }

    private static string? NormalizeSummary(string raw)
    {
        var s = (raw ?? "").Trim();
        if (s.Length == 0)
            return null;

        // Drop XML tags but keep inline <c>text</c>.
        s = s.Replace("<c>", "`", StringComparison.Ordinal).Replace("</c>", "`", StringComparison.Ordinal);
        s = Regex.Replace(s, "<.*?>", string.Empty);
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s.Length == 0 ? null : s;
    }
}

internal static class IdeCommandsLint
{
    public static IReadOnlyList<string> Run(IReadOnlyList<IdeCommandDoc> items)
    {
        var errors = new List<string>();

        foreach (var it in items.OrderBy(i => i.CommandId, StringComparer.Ordinal))
        {
            var s = it.Summary ?? "";

            if (!s.Contains("returns:", StringComparison.OrdinalIgnoreCase))
                errors.Add($"Missing returns: command_id='{it.CommandId}' ({it.FieldName})");
            else
            {
                try { _ = IdeCommandContractParser.ParseReturns(it.CommandId, it.Summary); }
                catch (Exception ex) { errors.Add($"Invalid returns: command_id='{it.CommandId}' ({it.FieldName}): {ex.Message}"); }
            }

            if (s.Contains("args:", StringComparison.OrdinalIgnoreCase) && !s.Contains("example:", StringComparison.OrdinalIgnoreCase))
                errors.Add($"Missing example (args present): command_id='{it.CommandId}' ({it.FieldName})");
            else if (s.Contains("example:", StringComparison.OrdinalIgnoreCase))
            {
                var ex = IdeCommandContractParser.ParseExample(it.Summary);
                if (!string.IsNullOrWhiteSpace(ex))
                {
                    try { JsonDocument.Parse(ex); }
                    catch (Exception ex2) { errors.Add($"Invalid example JSON: command_id='{it.CommandId}' ({it.FieldName}): {ex2.Message}"); }
                }
            }
        }

        return errors;
    }
}

internal static class IdeMcpCommandExecutorEmitter
{
    // First iteration: generate only "pure IIdeMcpActions pass-through" handlers.
    // UI-thread / RelayCommand based handlers remain manual in IdeMcpCommandExecutor.
    public static string Emit()
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using CascadeIDE.Services;");
        sb.AppendLine();
        sb.AppendLine("namespace CascadeIDE.ViewModels;");
        sb.AppendLine();
        sb.AppendLine("internal sealed partial class IdeMcpCommandExecutor");
        sb.AppendLine("{");
        sb.AppendLine("    partial void RegisterGenerated(Action<string, Handler> add)");
        sb.AppendLine("    {");

        // Workspace / solution info
        sb.AppendLine("        add(Services.IdeCommands.GetSolutionInfo, async (_, _) => await Task.FromResult(((IIdeMcpActions)_vm).GetSolutionInfo()));");
        sb.AppendLine("        add(Services.IdeCommands.GetWorkspaceState, async (_, _) => await ((IIdeMcpActions)_vm).GetWorkspaceStateAsync());");
        sb.AppendLine("        add(Services.IdeCommands.GetSolutionFiles, async (_, _) => await ((IIdeMcpActions)_vm).GetSolutionFilesAsync());");
        sb.AppendLine("        add(Services.IdeCommands.GetCurrentFileDiagnostics, async (_, _) => await ((IIdeMcpActions)_vm).GetCurrentFileDiagnosticsAsync());");

        // Build / test
        sb.AppendLine("        add(Services.IdeCommands.Build, async (_, _) => await ((IIdeMcpActions)_vm).BuildAsync());");
        sb.AppendLine("        add(Services.IdeCommands.BuildStructured, async (_, _) => await ((IIdeMcpActions)_vm).BuildStructuredAsync());");
        sb.AppendLine("        add(Services.IdeCommands.RunTests, async (_, _) => await ((IIdeMcpActions)_vm).RunTestsAsync());");
        sb.AppendLine("        add(Services.IdeCommands.RunAffectedTests, async (args, _) => await ((IIdeMcpActions)_vm).RunAffectedTestsAsync(JsonArgs.StringList(args, \"changed_paths\")));");
        sb.AppendLine("        add(Services.IdeCommands.RunCodeCleanup, async (args, _) => await ((IIdeMcpActions)_vm).RunCodeCleanupAsync(JsonArgs.String(args, \"include_path\")));");
        sb.AppendLine("        add(Services.IdeCommands.GetCodeMetrics, async (args, _) => await ((IIdeMcpActions)_vm).GetCodeMetricsAsync(JsonArgs.String(args, \"scope\"), JsonArgs.String(args, \"path\")));");

        // Git
        sb.AppendLine("        add(Services.IdeCommands.GitStatus, async (_, _) => await ((IIdeMcpActions)_vm).GitStatusAsync());");
        sb.AppendLine("        add(Services.IdeCommands.GitDiff, async (args, _) => await ((IIdeMcpActions)_vm).GitDiffAsync(JsonArgs.String(args, \"path\"), JsonArgs.Bool(args, \"staged\")));");
        sb.AppendLine("        add(Services.IdeCommands.GitCommit, async (args, _) =>");
        sb.AppendLine("        {");
        sb.AppendLine("            if (string.IsNullOrWhiteSpace(JsonArgs.String(args, \"message\"))) return \"Missing message\";");
        sb.AppendLine("            return await ((IIdeMcpActions)_vm).GitCommitAsync(JsonArgs.String(args, \"message\")!, JsonArgs.StringList(args, \"paths\"));");
        sb.AppendLine("        });");
        sb.AppendLine("        add(Services.IdeCommands.GitPush, async (args, _) => await ((IIdeMcpActions)_vm).GitPushAsync(JsonArgs.String(args, \"remote\"), JsonArgs.String(args, \"branch\")));");

        // Output / diagnostics
        sb.AppendLine("        add(Services.IdeCommands.GetBuildOutput, async (_, _) => await Task.FromResult(((IIdeMcpActions)_vm).GetBuildOutput()));");

        // UI inspection / control (pure IIdeMcpActions)
        sb.AppendLine("        add(Services.IdeCommands.GetUiTheme, async (_, _) => await Task.FromResult(((IIdeMcpActions)_vm).GetUiTheme()));");
        sb.AppendLine("        add(Services.IdeCommands.SetUiTheme, async (args, _) => await ((IIdeMcpActions)_vm).SetUiThemeAsync(JsonArgs.String(args, \"theme\") ?? \"\"));");
        sb.AppendLine("        add(Services.IdeCommands.GetUiLayout, async (_, _) => await ((IIdeMcpActions)_vm).GetUiLayoutAsync());");
        sb.AppendLine("        add(Services.IdeCommands.GetColorsUnderCursor, async (_, _) => await ((IIdeMcpActions)_vm).GetColorsUnderCursorAsync());");
        sb.AppendLine("        add(Services.IdeCommands.GetControlAppearance, async (args, _) => await ((IIdeMcpActions)_vm).GetControlAppearanceAsync(JsonArgs.String(args, \"name\")));");
        sb.AppendLine("        add(Services.IdeCommands.SetControlLayout, async (args, _) =>");
        sb.AppendLine("        {");
        sb.AppendLine("            if (args is null || string.IsNullOrEmpty(JsonArgs.String(args, \"name\"))) return \"Missing name or layout\";");
        sb.AppendLine("            return await ((IIdeMcpActions)_vm).SetControlLayoutAsync(JsonArgs.String(args, \"name\")!, JsonArgs.String(args, \"layout\") ?? \"{}\" );");
        sb.AppendLine("        });");
        sb.AppendLine("        add(Services.IdeCommands.SetControlText, async (args, _) => await ((IIdeMcpActions)_vm).SetControlTextAsync(JsonArgs.String(args, \"name\") ?? \"\", JsonArgs.String(args, \"text\") ?? \"\"));");
        sb.AppendLine("        add(Services.IdeCommands.ClickControl, async (args, _) => await ((IIdeMcpActions)_vm).ClickControlAsync(JsonArgs.String(args, \"name\")));");
        sb.AppendLine("        add(Services.IdeCommands.SendKeys, async (args, _) => await ((IIdeMcpActions)_vm).SendKeysAsync(JsonArgs.String(args, \"name\"), JsonArgs.String(args, \"keys\") ?? \"\"));");
        sb.AppendLine("        add(Services.IdeCommands.SetFocus, async (args, _) => await ((IIdeMcpActions)_vm).SetFocusAsync(JsonArgs.String(args, \"name\")));");
        sb.AppendLine("        add(Services.IdeCommands.HighlightControl, async (args, _) => await ((IIdeMcpActions)_vm).HighlightControlAsync(JsonArgs.String(args, \"name\")));");
        sb.AppendLine("        add(Services.IdeCommands.SetPanelSize, async (args, _) =>");
        sb.AppendLine("        {");
        sb.AppendLine("            double? w = args is not null && args.TryGetValue(\"width\", out var pw) && pw.TryGetDouble(out var wv) ? wv : null;");
        sb.AppendLine("            double? h = args is not null && args.TryGetValue(\"height\", out var ph) && ph.TryGetDouble(out var hv) ? hv : null;");
        sb.AppendLine("            return await ((IIdeMcpActions)_vm).SetPanelSizeAsync(JsonArgs.String(args, \"panel\") ?? \"\", w, h);");
        sb.AppendLine("        });");
        sb.AppendLine("        add(Services.IdeCommands.GetSupportedEditorLanguages, async (_, _) => await Task.FromResult(((IIdeMcpActions)_vm).GetSupportedEditorLanguages()));");

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }
}

internal static class IdeCommandArgsParser
{
    // Strict mini-grammar for args:
    // args: name:type, name?:type, name:type[], name?:type[]
    // where type is one of: string|integer|number|boolean|object
    private static readonly Regex ArgToken = new(
        @"^(?<name>[a-zA-Z_][a-zA-Z0-9_]*)(?<opt>\?)?\s*:\s*(?<type>string|integer|number|boolean|object)(?<arr>\[\])?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<IdeCommandArg> ParseArgsFromSummary(string commandId, string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return Array.Empty<IdeCommandArg>();

        var s = summary!;
        var idx = s.IndexOf("args:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return Array.Empty<IdeCommandArg>();

        var tail = s[(idx + 5)..].Trim();
        // Stop at first sentence end to avoid pulling extra prose.
        var end = tail.IndexOfAny(['.', ';']);
        if (end >= 0)
            tail = tail[..end];

        if (tail.Length == 0)
            return Array.Empty<IdeCommandArg>();

        var list = new List<IdeCommandArg>();
        foreach (var rawPart in tail.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var part = rawPart.Trim();
            if (part.Length == 0)
                continue;

            var match = ArgToken.Match(part);
            if (!match.Success)
                throw new InvalidOperationException($"Invalid args token for command '{commandId}': '{part}'. Expected 'name:type' with optional '?' and optional '[]'.");

            var name = match.Groups["name"].Value;
            var required = !match.Groups["opt"].Success;
            var jsonType = match.Groups["type"].Value;
            var isArray = match.Groups["arr"].Success;
            var itemType = isArray ? jsonType : null;

            list.Add(new IdeCommandArg(name, jsonType, required, isArray, itemType));
        }

        // de-dupe by name, prefer required=true
        return list
            .GroupBy(a => a.Name, StringComparer.Ordinal)
            .Select(g =>
            {
                var best = g.OrderByDescending(x => x.Required).ThenByDescending(x => x.IsArray).First();
                return best;
            })
            .ToList();
    }
}

internal enum IdeReturnKind
{
    Unspecified = 0,
    Text = 1,
    Json = 2,
    None = 3
}

internal static class IdeCommandContractParser
{
    private static readonly Regex ReturnsToken = new(@"^(?<kind>json|text|none)$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static IdeReturnKind ParseReturns(string commandId, string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return IdeReturnKind.Unspecified;

        var idx = summary!.IndexOf("returns:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return IdeReturnKind.Unspecified;

        var tail = summary[(idx + "returns:".Length)..].Trim();
        var end = tail.IndexOfAny(['.', ';']);
        if (end >= 0)
            tail = tail[..end];
        tail = tail.Trim();

        if (tail.Length == 0)
            throw new InvalidOperationException($"Invalid returns spec for command '{commandId}': empty. Expected returns: json|text|none.");

        var m = ReturnsToken.Match(tail);
        if (!m.Success)
            throw new InvalidOperationException($"Invalid returns spec for command '{commandId}': '{tail}'. Expected returns: json|text|none.");

        var kind = m.Groups["kind"].Value.ToLowerInvariant();
        return kind switch
        {
            "json" => IdeReturnKind.Json,
            "text" => IdeReturnKind.Text,
            "none" => IdeReturnKind.None,
            _ => IdeReturnKind.Unspecified
        };
    }

    public static string? ParseExample(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return null;

        var idx = summary!.IndexOf("example:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var tail = summary[(idx + "example:".Length)..].Trim();
        if (tail.Length == 0)
            return null;

        // If the example starts with JSON, extract the balanced JSON payload so
        // values like "." or Windows paths do not break "end of sentence" parsing.
        if (tail[0] is '{' or '[')
        {
            var json = TrySliceBalancedJson(tail);
            return string.IsNullOrWhiteSpace(json) ? null : json;
        }

        // Fallback: example goes to end of sentence.
        var end = tail.IndexOf('.');
        if (end >= 0)
            tail = tail[..end];

        tail = tail.Trim();
        return tail.Length == 0 ? null : tail;
    }

    private static string? TrySliceBalancedJson(string s)
    {
        // Minimal JSON slicer: balances {} / [] and respects strings + escapes.
        var open = s[0];
        var close = open == '{' ? '}' : ']';
        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            if (inString)
            {
                if (escape) { escape = false; continue; }
                if (ch == '\\') { escape = true; continue; }
                if (ch == '"') inString = false;
                continue;
            }

            if (ch == '"') { inString = true; continue; }
            if (ch == open) { depth++; continue; }
            if (ch == close)
            {
                depth--;
                if (depth == 0)
                    return s[..(i + 1)].Trim();
            }
        }

        return null;
    }
}

internal static class IdeCommandsContractEmitter
{
    public static string Emit(IReadOnlyList<IdeCommandDoc> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("namespace CascadeIDE.Services;");
        sb.AppendLine();
        sb.AppendLine("internal static class IdeCommandsContractGenerated");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly Dictionary<string, IdeReturnKind> ReturnsByCommandId = new(StringComparer.Ordinal)");
        sb.AppendLine("    {");
        foreach (var it in items.OrderBy(i => i.CommandId, StringComparer.Ordinal))
        {
            var kind = IdeCommandContractParser.ParseReturns(it.CommandId, it.Summary);
            if (kind == IdeReturnKind.Unspecified)
                continue;
            sb.Append("        [\"");
            sb.Append(Escape(it.CommandId));
            sb.Append("\"] = IdeReturnKind.");
            sb.Append(kind.ToString());
            sb.AppendLine(",");
        }
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    private static readonly Dictionary<string, string> ExampleByCommandId = new(StringComparer.Ordinal)");
        sb.AppendLine("    {");
        foreach (var it in items.OrderBy(i => i.CommandId, StringComparer.Ordinal))
        {
            var ex = IdeCommandContractParser.ParseExample(it.Summary);
            if (string.IsNullOrWhiteSpace(ex))
                continue;
            sb.Append("        [\"");
            sb.Append(Escape(it.CommandId));
            sb.Append("\"] = \"");
            sb.Append(Escape(ex!));
            sb.AppendLine("\",");
        }
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    public static bool TryGetReturns(string commandId, out IdeReturnKind kind) =>");
        sb.AppendLine("        ReturnsByCommandId.TryGetValue(commandId, out kind);");
        sb.AppendLine();
        sb.AppendLine("    public static bool TryGetExample(string commandId, out string example) =>");
        sb.AppendLine("        ExampleByCommandId.TryGetValue(commandId, out example);");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}

internal static class IdeCommandsDocEmitter
{
    public static string Emit(IReadOnlyList<IdeCommandDoc> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("namespace CascadeIDE.Services;");
        sb.AppendLine();
        sb.AppendLine("internal static class IdeCommandsDoc");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly Dictionary<string, string> SummaryByCommandId = new(StringComparer.Ordinal)");
        sb.AppendLine("    {");

        foreach (var it in items.OrderBy(i => i.CommandId, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(it.Summary))
                continue;
            sb.Append("        [\"");
            sb.Append(Escape(it.CommandId));
            sb.Append("\"] = \"");
            sb.Append(Escape(it.Summary!));
            sb.AppendLine("\",");
        }

        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    public static bool TryGetSummary(string commandId, out string summary) =>");
        sb.AppendLine("        SummaryByCommandId.TryGetValue(commandId, out summary);");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}

internal static class IdeCommandsArgsEmitter
{
    public static string Emit(IReadOnlyList<IdeCommandDoc> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("namespace CascadeIDE.Services;");
        sb.AppendLine();
        sb.AppendLine("internal static class IdeCommandsArgsGenerated");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly Dictionary<string, IdeCommandsArgs.Arg[]> ArgsByCommandId = new(StringComparer.Ordinal)");
        sb.AppendLine("    {");

        foreach (var it in items.OrderBy(i => i.CommandId, StringComparer.Ordinal))
        {
            var args = IdeCommandArgsParser.ParseArgsFromSummary(it.CommandId, it.Summary);
            if (args.Count == 0)
                continue;

            sb.Append("        [\"");
            sb.Append(Escape(it.CommandId));
            sb.Append("\"] = new IdeCommandsArgs.Arg[] { ");
            for (var i = 0; i < args.Count; i++)
            {
                var a = args[i];
                if (i > 0) sb.Append(", ");
                sb.Append("new(\"");
                sb.Append(Escape(a.Name));
                sb.Append("\", \"");
                sb.Append(Escape(a.JsonType));
                sb.Append("\", ");
                sb.Append(a.Required ? "true" : "false");
                sb.Append(", ");
                sb.Append(a.IsArray ? "true" : "false");
                sb.Append(", ");
                sb.Append(a.ItemJsonType is null ? "null" : $"\"{Escape(a.ItemJsonType)}\"");
                sb.Append(")");
            }
            sb.AppendLine(" },");
        }

        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    public static bool TryGetArgs(string commandId, out IdeCommandsArgs.Arg[] args) =>");
        sb.AppendLine("        ArgsByCommandId.TryGetValue(commandId, out args);");
        sb.AppendLine();
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}

internal static class ProtocolMdEmitter
{
    public static string EmitIdeCommandsTable(IReadOnlyList<IdeCommandDoc> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("> Этот блок сгенерирован из XML-doc в `Services/IdeCommands.cs`.");
        sb.AppendLine();

        foreach (var grp in items.GroupBy(i => i.Category, StringComparer.Ordinal))
        {
            sb.AppendLine($"### {grp.Key}");
            sb.AppendLine();
            sb.AppendLine("| command_id | Описание |");
            sb.AppendLine("|-----------:|----------|");
            foreach (var it in grp.OrderBy(i => i.CommandId, StringComparer.Ordinal))
            {
                var desc = it.Summary ?? "";
                sb.Append("| `");
                sb.Append(it.CommandId);
                sb.Append("` | ");
                sb.Append(EscapeMd(desc));
                sb.AppendLine(" |");
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd() + "\n";
    }

    private static string EscapeMd(string s) => (s ?? "").Replace("|", "\\|", StringComparison.Ordinal);
}

internal static class MarkerReplace
{
    public static string ReplaceSection(string text, string startMarker, string endMarker, string newContent)
    {
        var start = text.IndexOf(startMarker, StringComparison.Ordinal);
        var searchFrom = start < 0 ? 0 : start + startMarker.Length;
        var end = text.IndexOf(endMarker, searchFrom, StringComparison.Ordinal);
        if (start < 0 || end < 0 || end <= start)
            throw new InvalidOperationException("Markers not found or invalid order in MCP-PROTOCOL.md.");

        var insertAt = start + startMarker.Length;
        return text[..insertAt] + "\n" + newContent + text[end..];
    }
}

