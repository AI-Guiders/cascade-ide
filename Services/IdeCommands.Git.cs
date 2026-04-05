namespace CascadeIDE.Services;

/// <summary>Git в каталоге workspace (ide_execute_command).</summary>
public static partial class IdeCommands
{
    /// <summary>Git status в каталоге решения/workspace. returns: json.</summary>
    public const string GitStatus = "git_status";
    /// <summary>Git diff в каталоге решения/workspace. args: path?:string, staged?:boolean; returns: json; example: {"path":"README.md","staged":false}.</summary>
    public const string GitDiff = "git_diff";
    /// <summary>Git commit в каталоге решения/workspace. args: message:string, paths?:string[]; returns: text; example: {"message":"chore: update","paths":["a.txt"]}.</summary>
    public const string GitCommit = "git_commit";
    /// <summary>Git push в каталоге решения/workspace. args: remote?:string, branch?:string; returns: text; example: {"remote":"origin","branch":"main"}.</summary>
    public const string GitPush = "git_push";
}
