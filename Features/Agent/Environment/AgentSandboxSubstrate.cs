using System.Net;
using System.Net.Sockets;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>
/// Per-run substrate bundle (ADR 0148 §8.1.2): isolated DB path + ephemeral dev port.
/// Layout: <c>{runDirectory}/substrate/wit.db</c>, <c>port.txt</c>, <c>owner.txt</c>.
/// </summary>
public static class AgentSandboxSubstrate
{
    public static AgentSandboxSubstrateBundle Allocate(string runDirectory)
    {
        var substrateDir = Path.Combine(runDirectory, "substrate");
        Directory.CreateDirectory(substrateDir);

        var port = ReserveFreeTcpPort();
        var dbPath = Path.Combine(substrateDir, "wit.db");
        var markerPath = Path.Combine(substrateDir, "owner.txt");
        var portPath = Path.Combine(substrateDir, "port.txt");

        var ownerId = Path.GetFileName(runDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        File.WriteAllText(markerPath, ownerId);
        File.WriteAllText(portPath, port.ToString());
        File.WriteAllText(dbPath, ownerId);

        return new AgentSandboxSubstrateBundle(port, dbPath, markerPath, portPath, substrateDir);
    }

    public static void WriteHeavyMarker(AgentSandboxSubstrateBundle bundle, string payload)
    {
        Directory.CreateDirectory(bundle.SubstrateDirectory);
        File.WriteAllText(bundle.DatabasePath, payload);
        File.AppendAllText(bundle.MarkerPath, "|" + payload);
    }

    public static string ReadDatabaseOwner(AgentSandboxSubstrateBundle bundle) =>
        File.ReadAllText(bundle.DatabasePath);

    private static int ReserveFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}

public sealed record AgentSandboxSubstrateBundle(
    int DevPort,
    string DatabasePath,
    string MarkerPath,
    string PortFilePath,
    string SubstrateDirectory);
