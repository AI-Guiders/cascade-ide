using System.Security.Cryptography;
using System.Text;

namespace CascadeIDE.Features.Agent.Environment;

public static class VerifySnapshot
{
    public static string Create(string solutionPath)
    {
        var normalized = Path.GetFullPath(solutionPath.Trim());
        var tick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var payload = normalized + "|" + tick;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))[..16];
        return hash;
    }
}
