using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: Git (см. <c>IdeCommands.Git.cs</c>).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterGitPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        // ——— Git
        AddPalette(b, "git_status", IdeCommands.GitStatus, "Git: статус", "Git");
        AddPalette(b, "git_commit", IdeCommands.GitCommit, "Git: commit", "Git");
        AddPalette(b, "git_push", IdeCommands.GitPush, "Git: push", "Git");
        AddPalette(b, "git_submodule", IdeCommands.GitSubmodule, "Git: submodule", "Git");
    }
}
