#nullable enable

using System.Diagnostics;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>Best-effort git/solution snapshot @ send (ADR 0128 §3.1).</summary>
public static class IntercomSenderWorkspaceContextCapture
{
    public static SenderWorkspaceContext? TryCapture(string? workspaceRoot, string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        var root = workspaceRoot.Trim();
        if (!Directory.Exists(root))
            return null;

        var branch = tryGit(root, "rev-parse --abbrev-ref HEAD");
        var commit = tryGit(root, "rev-parse --short HEAD");
        var relSolution = string.IsNullOrWhiteSpace(solutionPath)
            ? null
            : AttachmentAnchorPaths.ToWorkspaceRelative(solutionPath, root) ?? solutionPath;

        if (branch is null && commit is null && relSolution is null)
            return null;

        return new SenderWorkspaceContext(
            branch,
            commit,
            relSolution,
            DateTimeOffset.UtcNow.ToString("O"));
    }

    private static string? tryGit(string workspaceRoot, string args)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = args,
                    WorkingDirectory = workspaceRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
            if (!process.Start())
                return null;
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(2000);
            return process.ExitCode == 0 && output.Length > 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
