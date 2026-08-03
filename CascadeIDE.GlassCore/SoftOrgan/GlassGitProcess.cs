#nullable enable

using System.Diagnostics;
using System.Text;

namespace CascadeIDE.SoftOrgan;

/// <summary>Thin redirected <c>git</c> for Glass MFD (stage/unstage/commit) — no SoftOrgan invent, no Avalonia GitPanel fork.</summary>
public static class GlassGitProcess
{
    public readonly record struct Result(bool Ok, int ExitCode, string Output);

    public static Result Run(string? workspaceRoot, params string[] args)
    {
        var cwd = string.IsNullOrWhiteSpace(workspaceRoot)
            ? Environment.CurrentDirectory
            : workspaceRoot.Trim();

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = cwd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-C");
        psi.ArgumentList.Add(cwd);
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        try
        {
            using var p = Process.Start(psi);
            if (p is null)
                return new Result(false, -1, "git failed to start");

            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            if (!p.WaitForExit(60_000))
            {
                try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return new Result(false, -1, "git timed out");
            }

            var buf = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(stdout))
                buf.Append(stdout.TrimEnd());
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                if (buf.Length > 0)
                    buf.AppendLine();
                buf.Append(stderr.TrimEnd());
            }

            return new Result(p.ExitCode == 0, p.ExitCode, buf.ToString());
        }
        catch (Exception ex)
        {
            return new Result(false, -1, ex.Message);
        }
    }
}
