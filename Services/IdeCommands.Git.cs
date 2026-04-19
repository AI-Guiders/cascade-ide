namespace CascadeIDE.Services;

/// <summary>Git в каталоге workspace (ide_execute_command). Аргументы — как в git-mcp (GitMcp.Core).</summary>
public static partial class IdeCommands
{
    /// <summary>Git status в каталоге решения/workspace (git status --short --branch). returns: json.</summary>
    public const string GitStatus = "git_status";
    /// <summary>Git diff в каталоге решения/workspace. args: path?:string, staged?:boolean; returns: json; example: {"path":"README.md","staged":false}.</summary>
    public const string GitDiff = "git_diff";
    /// <summary>Git log в каталоге решения/workspace. args: n?:integer; returns: json; example: {"n":20}.</summary>
    public const string GitLog = "git_log";
    /// <summary>Git fetch в каталоге решения/workspace. args: remote?:string, all?:boolean, prune?:boolean, dry_run?:boolean; returns: json; example: {"prune":true,"dry_run":true}.</summary>
    public const string GitFetch = "git_fetch";
    /// <summary>Git pull в каталоге решения/workspace. args: remote?:string, branch?:string, ff_only?:boolean, dry_run?:boolean; returns: json; example: {"ff_only":true}.</summary>
    public const string GitPull = "git_pull";
    /// <summary>Git branch в каталоге решения/workspace. args: action?:string, name?:string, start_point?:string, force?:boolean; returns: json; example: {"action":"list"}.</summary>
    public const string GitBranch = "git_branch";
    /// <summary>Git show в каталоге решения/workspace. args: rev:string, path?:string, stat_only?:boolean; returns: json; example: {"rev":"HEAD","stat_only":true}.</summary>
    public const string GitShow = "git_show";
    /// <summary>Git submodule в каталоге решения/workspace. args: action?:string, path?:string, recursive?:boolean; returns: json; example: {"action":"status"}.</summary>
    public const string GitSubmodule = "git_submodule";
    /// <summary>Git commit в каталоге решения/workspace. args: message:string, paths?:string[]; returns: text; example: {"message":"chore: update","paths":["a.txt"]}.</summary>
    public const string GitCommit = "git_commit";
    /// <summary>Git push в каталоге решения/workspace. args: remote?:string, branch?:string, dry_run?:boolean; returns: text; example: {"remote":"origin","branch":"main","dry_run":true}.</summary>
    public const string GitPush = "git_push";
}
