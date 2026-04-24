#!/usr/bin/env dotnet script
// Запуск (из каталога cascade-ide или с путём к скрипту):
//   dotnet script docs/samples/agent-contract-ci.csx -- --exe path\to\CascadeIDE.exe
//   dotnet script docs/samples/agent-contract-ci.csx -- --exe path\to\CascadeIDE.exe --workspace D:\repo
// Один раз: dotnet tool install -g dotnet-script (как в Financial/finplan, agents-and-humans-book).

using System.Diagnostics;
using System.Text;
using System.Text.Json;

static string[] GetArgsAfterCsx()
{
    var a = Environment.GetCommandLineArgs();
    for (var i = 0; i < a.Length; i++)
    {
        if (!a[i].EndsWith(".csx", StringComparison.OrdinalIgnoreCase) || i + 1 >= a.Length)
            continue;
        var tail = a.AsSpan(i + 1).ToArray();
        // dotnet script foo.csx -- --exe … → после .csx часто идёт разделитель "--"
        if (tail.Length > 0 && tail[0] == "--")
            return tail.AsSpan(1).ToArray();
        return tail;
    }
    return Array.Empty<string>();
}

static int RunAgentContract(string exe, string[] arguments, out string stdout, out string stderr)
{
    stdout = "";
    stderr = "";
    var psi = new ProcessStartInfo(exe)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
    };
    foreach (var arg in arguments)
        psi.ArgumentList.Add(arg);
    using var p = Process.Start(psi);
    if (p is null)
        return -1;
    stdout = p.StandardOutput.ReadToEnd();
    stderr = p.StandardError.ReadToEnd();
    p.WaitForExit();
    return p.ExitCode;
}

var tail = GetArgsAfterCsx();
string cascadeExe = null;
var workspace = Environment.CurrentDirectory;
for (var i = 0; i < tail.Length; i++)
{
    if (string.Equals(tail[i], "--exe", StringComparison.OrdinalIgnoreCase))
    {
        if (i + 1 >= tail.Length) { Console.Error.WriteLine("--exe requires a path."); Environment.Exit(2); }
        cascadeExe = tail[++i];
        continue;
    }
    if (string.Equals(tail[i], "--workspace", StringComparison.OrdinalIgnoreCase))
    {
        if (i + 1 >= tail.Length) { Console.Error.WriteLine("--workspace requires a path."); Environment.Exit(2); }
        workspace = tail[++i];
        continue;
    }
    Console.Error.WriteLine($"Unknown argument: {tail[i]}");
    Environment.Exit(2);
}

if (string.IsNullOrWhiteSpace(cascadeExe) || !File.Exists(cascadeExe))
{
    Console.Error.WriteLine("Usage: dotnet script agent-contract-ci.csx -- --exe <path\\to\\CascadeIDE.exe> [--workspace <repoRoot>]");
    Console.Error.WriteLine("Requires a built/published CascadeIDE.exe.");
    Environment.Exit(2);
}

string json;
int code;

code = RunAgentContract(cascadeExe, ["--agent-contract", "get_ui_modes_diagnostics"], out json, out var err);
if (code != 0) { Console.Error.WriteLine(err); Environment.Exit(code); }
if (string.IsNullOrWhiteSpace(json)) { Console.Error.WriteLine("empty stdout from get_ui_modes_diagnostics"); Environment.Exit(3); }

code = RunAgentContract(cascadeExe, ["--agent-contract", "get_supported_editor_languages"], out json, out err);
if (code != 0) { Console.Error.WriteLine(err); Environment.Exit(code); }

code = RunAgentContract(cascadeExe, ["--agent-contract", "get_solution_info"], out json, out err);
if (code != 0) { Console.Error.WriteLine(err); Environment.Exit(code); }
try { JsonDocument.Parse(json); }
catch (Exception ex) { Console.Error.WriteLine($"get_solution_info: invalid JSON: {ex.Message}"); Environment.Exit(3); }

code = RunAgentContract(cascadeExe, ["--agent-contract", "get_cockpit_surface"], out json, out err);
if (code != 0) { Console.Error.WriteLine(err); Environment.Exit(code); }
try { JsonDocument.Parse(json); }
catch (Exception ex) { Console.Error.WriteLine($"get_cockpit_surface: invalid JSON: {ex.Message}"); Environment.Exit(3); }

code = RunAgentContract(cascadeExe, ["--agent-contract", "get_ide_state"], out json, out err);
if (code != 0) { Console.Error.WriteLine(err); Environment.Exit(code); }
try
{
    using var doc = JsonDocument.Parse(json);
    if (!doc.RootElement.TryGetProperty("cockpit_surface", out _))
    {
        Console.Error.WriteLine("get_ide_state: missing cockpit_surface");
        Environment.Exit(3);
    }
}
catch (Exception ex) { Console.Error.WriteLine($"get_ide_state: invalid JSON: {ex.Message}"); Environment.Exit(3); }

code = RunAgentContract(cascadeExe, ["--agent-contract", "--workspace", workspace, "git_status"], out json, out err);
if (code != 0) { Console.Error.WriteLine(err); Environment.Exit(code); }

Console.WriteLine("OK: agent-contract checks passed (ui_modes, languages, solution_info, cockpit, ide_state, git_status).");
Environment.Exit(0);
