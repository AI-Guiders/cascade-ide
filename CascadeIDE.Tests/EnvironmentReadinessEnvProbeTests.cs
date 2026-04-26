using CascadeIDE.Cockpit.Composition.EnvironmentReadiness;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EnvironmentReadinessEnvProbeTests
{
    [Fact]
    public void BuildEnvProbeRows_unset_notes_ok_canon_and_dbg_advisory()
    {
        var rows = EnvironmentReadinessSnapshotBuilder.BuildEnvProbeRows(
            new EnvironmentReadinessEnvSnapshot(null, null, null),
            tryResolveNetcoreDbgWhenUnset: static () => null);

        Assert.Equal(3, rows.Count);
        Assert.Equal(EnvironmentReadinessCellIds.AgentNotesFile, rows[0].Id);
        Assert.Equal(WellKnownEnv.AgentNotesFile, rows[0].Title);
        Assert.Equal(AnnunciatorLampLevel.Ok, rows[0].Level);

        Assert.Equal(EnvironmentReadinessCellIds.AgentNotesCanonPath, rows[1].Id);
        Assert.Equal(AnnunciatorLampLevel.Advisory, rows[1].Level);

        Assert.Equal(EnvironmentReadinessCellIds.NetcoreDbgPath, rows[2].Id);
        Assert.Equal(AnnunciatorLampLevel.Advisory, rows[2].Level);
    }

    [Fact]
    public void BuildEnvProbeRows_unset_netcoredbg_resolved_from_path_ok()
    {
        var rows = EnvironmentReadinessSnapshotBuilder.BuildEnvProbeRows(
            new EnvironmentReadinessEnvSnapshot(null, null, null),
            tryResolveNetcoreDbgWhenUnset: static () => @"C:\fake\netcoredbg.exe");

        var dbg = Assert.Single(rows, r => r.Id == EnvironmentReadinessCellIds.NetcoreDbgPath);
        Assert.Equal(AnnunciatorLampLevel.Ok, dbg.Level);
        Assert.Contains("PATH", dbg.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvProbeRows_canon_existing_directory_ok()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cascade-ers-canon-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(dir);
        try
        {
            var rows = EnvironmentReadinessSnapshotBuilder.BuildEnvProbeRows(
                new EnvironmentReadinessEnvSnapshot(null, dir, null));

            var canon = Assert.Single(rows, r => r.Id == EnvironmentReadinessCellIds.AgentNotesCanonPath);
            Assert.Equal(AnnunciatorLampLevel.Ok, canon.Level);
        }
        finally
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // temp cleanup best-effort
            }
        }
    }

    [Fact]
    public void BuildEnvProbeRows_netcoredbg_bare_name_not_on_path_is_caution()
    {
        var rows = EnvironmentReadinessSnapshotBuilder.BuildEnvProbeRows(
            new EnvironmentReadinessEnvSnapshot(null, null, "zzz_cascade_ers_missing_exe_9f2a"));

        var dbg = Assert.Single(rows, r => r.Id == EnvironmentReadinessCellIds.NetcoreDbgPath);
        Assert.Equal(AnnunciatorLampLevel.Caution, dbg.Level);
    }

    [Fact]
    public void BuildEnvProbeRows_netcoredbg_existing_file_path_ok()
    {
        var temp = Path.Combine(Path.GetTempPath(), "cascade-ers-dbg-" + Guid.NewGuid().ToString("n") + ".exe");
        File.WriteAllText(temp, "");
        try
        {
            var rows = EnvironmentReadinessSnapshotBuilder.BuildEnvProbeRows(
                new EnvironmentReadinessEnvSnapshot(null, null, temp));

            var dbg = Assert.Single(rows, r => r.Id == EnvironmentReadinessCellIds.NetcoreDbgPath);
            Assert.Equal(AnnunciatorLampLevel.Ok, dbg.Level);
        }
        finally
        {
            try
            {
                File.Delete(temp);
            }
            catch
            {
                // best-effort
            }
        }
    }
}
