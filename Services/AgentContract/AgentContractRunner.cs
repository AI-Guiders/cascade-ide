#nullable enable
using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Features.UiChrome;
using GitMcp.Core;

namespace CascadeIDE.Services.AgentContract;

/// <summary>
/// Headless вывод JSON контракта агента в stdout (ADR 0052): те же сборщики, что и MCP <c>ide_*</c>, без stdio MCP.
/// Запуск: <c>--agent-contract [--workspace &lt;dir&gt;] &lt;command&gt;</c> (см. <see cref="PrintHelp"/>).
/// </summary>
public static class AgentContractRunner
{
    private static readonly IGitCommandRunner GitRunner = new GitCommandRunner();

    /// <summary>Возвращает код выхода: 0 — ок, 1 — help/нет команды, 2 — ошибка.</summary>
    public static int Run(ReadOnlySpan<string> args)
    {
        var argv = args.Length == 0 ? Array.Empty<string>() : args.ToArray();
        return RunAsync(argv).GetAwaiter().GetResult();
    }

    /// <summary>Тот же JSON, что вернёт MCP для соответствующей команды (для тестов и внешних вызовов).</summary>
    public static string GetContractJson(string command)
    {
        if (!TryGetJson(new[] { command }, out var json, out var error) || json is null)
            throw new InvalidOperationException(error ?? "Unknown command.");

        return json;
    }

    /// <summary>Полный argv после <c>--agent-contract</c> (включая <c>--workspace</c> и хвост для git).</summary>
    public static string GetContractJson(IReadOnlyList<string> argv)
    {
        var arr = argv is string[] a ? a : argv.ToArray();
        if (!TryGetJson(arr, out var json, out var error) || json is null)
            throw new InvalidOperationException(error ?? "Unknown command.");

        return json;
    }

    public static bool TryGetJson(string[] args, [NotNullWhen(true)] out string? json, [NotNullWhen(false)] out string? error)
    {
        var r = TryGetJsonAsync(args, CancellationToken.None).GetAwaiter().GetResult();
        json = r.Json;
        error = r.Error;
        return r.Ok;
    }

    public static async Task<(bool Ok, string? Json, string? Error)> TryGetJsonAsync(
        string[] args,
        CancellationToken cancellationToken = default)
    {
        if (args.Length == 0)
            return (false, null, "No command.");

        if (!TryParseWorkspaceAndPositionals(args, out var workspace, out var positionals, out var parseError))
            return (false, null, parseError);

        if (positionals.Length == 0)
            return (false, null, "No command.");

        var command = positionals[0];
        if (IsHelp(command))
            return (false, null, null); // Run maps (false, null, null) to PrintHelp + exit 0

        var tail = positionals.Length > 1 ? positionals.AsSpan(1).ToArray() : Array.Empty<string>();

        try
        {
            var json = await TryBuildJsonAsync(workspace, command, tail, cancellationToken).ConfigureAwait(false);
            return (true, json, null);
        }
        catch (InvalidOperationException ex)
        {
            return (false, null, ex.Message);
        }
    }

    private static async Task<string> TryBuildJsonAsync(
        string? workspace,
        string command,
        string[] tail,
        CancellationToken cancellationToken)
    {
        switch (command)
        {
            case IdeCommands.GetUiModesDiagnostics:
                RequireEmptyTail(tail, command);
                UiModeCatalog.Initialize();
                return UiModeCatalog.GetDiagnosticsJson();

            case IdeCommands.GetSupportedEditorLanguages:
                RequireEmptyTail(tail, command);
                return EditorLanguageSupport.GetJson();

            case IdeCommands.GitStatus:
                RequireEmptyTail(tail, command);
                return await GitStatusAsync(workspace, cancellationToken).ConfigureAwait(false);

            case IdeCommands.GitDiff:
                return await GitDiffAsync(workspace, tail, cancellationToken).ConfigureAwait(false);

            case IdeCommands.GitLog:
                return await GitLogAsync(workspace, tail, cancellationToken).ConfigureAwait(false);

            case IdeCommands.GitBranch:
                return await GitBranchAsync(workspace, tail, cancellationToken).ConfigureAwait(false);

            case IdeCommands.GitShow:
                return await GitShowAsync(workspace, tail, cancellationToken).ConfigureAwait(false);

            default:
                throw new InvalidOperationException(
                    $"Unknown agent contract command: {command}. See --agent-contract --help.");
        }
    }

    private static void RequireEmptyTail(string[] tail, string command)
    {
        if (tail.Length > 0)
            throw new InvalidOperationException($"{command}: unexpected arguments after command.");
    }

    private static string ResolveWorkspaceRoot(string? workspace)
    {
        var root = string.IsNullOrWhiteSpace(workspace) ? Environment.CurrentDirectory : workspace.Trim();
        if (!Directory.Exists(root))
            throw new InvalidOperationException($"Workspace path does not exist: {root}");
        return root;
    }

    private static async Task<string> GitStatusAsync(string? workspace, CancellationToken cancellationToken)
    {
        var root = ResolveWorkspaceRoot(workspace);
        return await AgentContractGitJson.RunAsync(
            GitRunner,
            GitCommandBuilder.StatusShortBranch(),
            root,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> GitDiffAsync(string? workspace, string[] tail, CancellationToken cancellationToken)
    {
        if (!TryParseGitDiffTail(tail, out var path, out var staged, out var err))
            throw new InvalidOperationException(err);
        var root = ResolveWorkspaceRoot(workspace);
        return await AgentContractGitJson.RunAsync(
            GitRunner,
            GitCommandBuilder.Diff(staged, path),
            root,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> GitLogAsync(string? workspace, string[] tail, CancellationToken cancellationToken)
    {
        if (!TryParseGitLogTail(tail, out var n, out var err))
            throw new InvalidOperationException(err);
        var root = ResolveWorkspaceRoot(workspace);
        return await AgentContractGitJson.RunAsync(
            GitRunner,
            GitCommandBuilder.Log(n),
            root,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> GitBranchAsync(string? workspace, string[] tail, CancellationToken cancellationToken)
    {
        if (tail.Length > 0)
            throw new InvalidOperationException("git_branch list only: remove extra arguments.");
        var root = ResolveWorkspaceRoot(workspace);
        var r = GitCommandBuilder.BranchList();
        if (!r.IsSuccess || r.Args is null)
            throw new InvalidOperationException(r.Error ?? "git_branch: invalid args.");
        return await AgentContractGitJson.RunAsync(GitRunner, r.Args, root, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> GitShowAsync(string? workspace, string[] tail, CancellationToken cancellationToken)
    {
        if (!TryParseGitShowTail(tail, out var rev, out var path, out var statOnly, out var err))
            throw new InvalidOperationException(err);
        var root = ResolveWorkspaceRoot(workspace);
        var r = GitCommandBuilder.Show(rev, path, statOnly);
        if (!r.IsSuccess || r.Args is null)
            throw new InvalidOperationException(r.Error ?? "git_show: invalid args.");
        return await AgentContractGitJson.RunAsync(GitRunner, r.Args, root, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryParseGitDiffTail(string[] tail, out string? path, out bool staged, [NotNullWhen(false)] out string? error)
    {
        path = null;
        staged = false;
        error = null;
        for (var i = 0; i < tail.Length; i++)
        {
            var t = tail[i];
            if (string.Equals(t, "--staged", StringComparison.OrdinalIgnoreCase))
            {
                staged = true;
                continue;
            }

            if (string.Equals(t, "--path", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= tail.Length)
                {
                    error = "--path requires a value.";
                    return false;
                }

                path = tail[++i];
                continue;
            }

            error = $"Unexpected argument for git_diff: {t}";
            return false;
        }

        return true;
    }

    private static bool TryParseGitLogTail(string[] tail, out int n, [NotNullWhen(false)] out string? error)
    {
        n = GitCommandBuilder.LogCountDefault;
        error = null;
        for (var i = 0; i < tail.Length; i++)
        {
            if (string.Equals(tail[i], "--n", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tail[i], "-n", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= tail.Length || !int.TryParse(tail[i + 1], out var v)) { error = "--n requires an integer."; return false; }
                n = v;
                i++;
                continue;
            }

            error = $"Unexpected argument for git_log: {tail[i]}";
            return false;
        }

        return true;
    }

    private static bool TryParseGitShowTail(
        string[] tail,
        [NotNullWhen(true)] out string? rev,
        out string? path,
        out bool statOnly,
        [NotNullWhen(false)] out string? error)
    {
        rev = null;
        path = null;
        statOnly = false;
        error = null;
        for (var i = 0; i < tail.Length; i++)
        {
            var t = tail[i];
            if (string.Equals(t, "--stat-only", StringComparison.OrdinalIgnoreCase))
            {
                statOnly = true;
                continue;
            }

            if (string.Equals(t, "--rev", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= tail.Length)
                {
                    error = "--rev requires a value.";
                    return false;
                }

                rev = tail[++i];
                continue;
            }

            if (string.Equals(t, "--path", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= tail.Length)
                {
                    error = "--path requires a value.";
                    return false;
                }

                path = tail[++i];
                continue;
            }

            error = $"Unexpected argument for git_show: {t}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(rev))
        {
            error = "git_show requires --rev <rev>.";
            return false;
        }

        return true;
    }

    /// <summary>Только <c>--workspace</c> на верхнем уровне; далее команда и хвост (в т.ч. <c>--n</c>, <c>--path</c>).</summary>
    private static bool TryParseWorkspaceAndPositionals(
        string[] args,
        out string? workspace,
        [NotNullWhen(true)] out string[]? positionals,
        [NotNullWhen(false)] out string? error)
    {
        workspace = null;
        positionals = null;
        error = null;
        var rest = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (string.Equals(a, "--workspace", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    error = "--workspace requires a path.";
                    return false;
                }

                workspace = args[++i];
                continue;
            }

            if (a.StartsWith("--workspace=", StringComparison.OrdinalIgnoreCase))
            {
                var v = a["--workspace=".Length..];
                if (string.IsNullOrEmpty(v))
                {
                    error = "--workspace= requires a value.";
                    return false;
                }

                workspace = v;
                continue;
            }

            rest.Add(a);
        }

        positionals = rest.ToArray();
        return true;
    }

    private static bool IsHelp(string arg) =>
        string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
        || string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase);

    private static void PrintHelp()
    {
        Console.Out.WriteLine(
            """
            CascadeIDE --agent-contract [--workspace <dir>] <command>
            Print the same JSON as the matching ide_* MCP tool (ADR 0052). No GUI; no MCP stdio.
            --workspace <dir>   Root for git_* commands (default: current directory).

            Commands (no workspace):
              get_supported_editor_languages   Same payload as ide_get_supported_editor_languages
              get_ui_modes_diagnostics        Same payload as ide_get_ui_modes_diagnostics

            Commands (git — same JSON as ide_git_*):
              git_status
              git_diff [--path <file>] [--staged]
              git_log [--n <count>]
              git_branch                       (list only)
              git_show --rev <rev> [--path <file>] [--stat-only]

            Examples:
              CascadeIDE.exe --agent-contract get_ui_modes_diagnostics
              CascadeIDE.exe --agent-contract --workspace D:\repo git_status
            CI: dotnet script docs/samples/agent-contract-ci.csx -- --exe path\to\CascadeIDE.exe
            Or PowerShell: docs/samples/agent-contract-ci.ps1
            """);
    }

    private static async Task<int> RunAsync(string[] argv)
    {
        if (argv.Length == 0)
        {
            PrintHelp();
            return 1;
        }

        if (IsHelp(argv[0]))
        {
            PrintHelp();
            return 0;
        }

        var r = await TryGetJsonAsync(argv, CancellationToken.None).ConfigureAwait(false);
        if (!r.Ok)
        {
            if (r.Error is null)
            {
                PrintHelp();
                return 0;
            }

            Console.Error.WriteLine(r.Error);
            return 2;
        }

        Console.Out.WriteLine(r.Json);
        return 0;
    }
}
