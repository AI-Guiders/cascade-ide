using CascadeIDE.Services.CursorAcp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CursorAcpAgentPathTests
{
    private static readonly object EnvLock = new();

    [Fact]
    public void TryResolve_uses_PATH_when_configured_is_empty()
    {
        lock (EnvLock)
        {
            var oldPath = Environment.GetEnvironmentVariable("PATH");
            var oldPathExt = Environment.GetEnvironmentVariable("PATHEXT");
            var tmp = Directory.CreateTempSubdirectory("cascade_cursor_acp_path_");
            try
            {
                var cmd = Path.Combine(tmp.FullName, "cursor-agent.cmd");
                File.WriteAllText(cmd, "@echo off\r\n");

                Environment.SetEnvironmentVariable("PATH", tmp.FullName);
                Environment.SetEnvironmentVariable("PATHEXT", ".CMD;.EXE;.BAT");

                var ok = CursorAcpAgentPath.TryResolve("", out var cmdPath, out var workDir);

                Assert.True(ok);
                Assert.Equal(Path.GetFullPath(cmd), cmdPath, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(Path.GetFullPath(tmp.FullName), Path.GetFullPath(workDir));
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", oldPath);
                Environment.SetEnvironmentVariable("PATHEXT", oldPathExt);
                tmp.Delete(recursive: true);
            }
        }
    }

    [Fact]
    public void TryResolve_returns_false_when_empty_and_not_in_PATH()
    {
        lock (EnvLock)
        {
            var oldPath = Environment.GetEnvironmentVariable("PATH");
            try
            {
                Environment.SetEnvironmentVariable("PATH", "");
                var ok = CursorAcpAgentPath.TryResolve("", out var cmdPath, out var workDir);
                Assert.False(ok);
                Assert.Equal("", cmdPath);
                Assert.Equal("", workDir);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", oldPath);
            }
        }
    }
}
