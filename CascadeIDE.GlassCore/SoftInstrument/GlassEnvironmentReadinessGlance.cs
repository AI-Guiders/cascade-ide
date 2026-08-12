#nullable enable
using System.Text;

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// Glass MFD EnvironmentReadiness: env/PATH presence glance (no SoftInstrument invent).
/// Live annunciator SSOT stays Avalonia EnvironmentReadiness CCU (ADR 0023).
/// </summary>
public static class GlassEnvironmentReadinessGlance
{
    public const string AgentNotesFileEnv = "AGENT_NOTES_FILE";
    public const string NetcoreDbgPathEnv = "NETCOREDBG_PATH";

    public readonly record struct EnvProbeRow(string Name, string State, string? Detail);

    public readonly record struct EnvProbeStatus(
        EnvProbeRow AgentNotes,
        EnvProbeRow NetcoreDbg,
        EnvProbeRow Dotnet);

    public static EnvProbeStatus ProbeCurrentProcess()
    {
        return new EnvProbeStatus(
            ProbePathEnv(AgentNotesFileEnv, allowDirectory: true),
            ProbePathEnv(NetcoreDbgPathEnv, allowDirectory: false),
            ProbeDotnetOnPath());
    }

    public static string TryFormatCurrentProcess() => Format(ProbeCurrentProcess());

    /// <summary>Testable formatter (no I/O).</summary>
    public static string Format(EnvProbeStatus status)
    {
        var level = Aggregate(status);
        var sb = new StringBuilder();
        sb.Append("EnvironmentReadiness glance · ").AppendLine(level);
        AppendRow(sb, status.AgentNotes);
        AppendRow(sb, status.NetcoreDbg);
        AppendRow(sb, status.Dotnet);

        if (level is "DEGRADED" or "MISSING")
            sb.AppendLine("· live lamps = Avalonia EnvironmentReadiness");

        sb.AppendLine();
        sb.AppendLine("┌ host ──────────────┐");
        sb.AppendLine("│ ■ Glass env probe    │");
        sb.AppendLine("│ □ Avalonia EnvReady  │");
        sb.AppendLine("└─────────────────────┘");
        return sb.ToString().TrimEnd();
    }

    static string Aggregate(EnvProbeStatus status)
    {
        static int Rank(string state) => state switch
        {
            "ok" => 0,
            "unset" => 1,
            "missing" => 2,
            _ => 1,
        };

        var worst = Math.Max(
            Rank(status.AgentNotes.State),
            Math.Max(Rank(status.NetcoreDbg.State), Rank(status.Dotnet.State)));

        // Notes/Dbg unset is advisory; missing file or no dotnet is degraded.
        if (status.Dotnet.State == "missing")
            return "MISSING";
        if (status.AgentNotes.State == "missing" || status.NetcoreDbg.State == "missing")
            return "DEGRADED";
        return worst <= 1 ? "READY" : "DEGRADED";
    }

    static void AppendRow(StringBuilder sb, EnvProbeRow row)
    {
        sb.Append(row.Name).Append(" · ").Append(row.State);
        if (!string.IsNullOrWhiteSpace(row.Detail))
            sb.Append(" · ").Append(row.Detail);
        sb.AppendLine();
    }

    static EnvProbeRow ProbePathEnv(string envName, bool allowDirectory)
    {
        var raw = Environment.GetEnvironmentVariable(envName);
        if (string.IsNullOrWhiteSpace(raw))
            return new EnvProbeRow(envName, "unset", null);

        try
        {
            var path = raw.Trim().Trim('"');
            var exists = File.Exists(path) || (allowDirectory && Directory.Exists(path));
            return exists
                ? new EnvProbeRow(envName, "ok", ShortLeaf(path))
                : new EnvProbeRow(envName, "missing", ShortLeaf(path));
        }
        catch
        {
            return new EnvProbeRow(envName, "missing", "?");
        }
    }

    static EnvProbeRow ProbeDotnetOnPath()
    {
        var found = TryFindOnPath("dotnet");
        return found is null
            ? new EnvProbeRow("dotnet", "missing", "not on PATH")
            : new EnvProbeRow("dotnet", "ok", ShortLeaf(found));
    }

    static string? TryFindOnPath(string fileName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
            return null;

        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var d = dir.Trim().Trim('"');
            if (d.Length == 0)
                continue;

            foreach (var name in CandidateNames(fileName))
            {
                string p;
                try
                {
                    p = Path.Combine(d, name);
                }
                catch
                {
                    continue;
                }

                if (File.Exists(p))
                    return p;
            }
        }

        return null;
    }

    static IEnumerable<string> CandidateNames(string fileName)
    {
        if (OperatingSystem.IsWindows() && !Path.HasExtension(fileName))
        {
            yield return fileName + ".exe";
            yield return fileName + ".cmd";
            yield return fileName + ".bat";
            yield return fileName;
        }
        else
        {
            yield return fileName;
        }
    }

    static string ShortLeaf(string path)
    {
        try
        {
            return Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        catch
        {
            return path;
        }
    }
}
