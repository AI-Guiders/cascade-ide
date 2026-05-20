#nullable enable

using System.Security.Cryptography;
using System.Text;

namespace CascadeIDE.Services.Intercom;

/// <summary>Стабильный ключ кэша attach-resolve для пары workspace + solution (ADR 0135).</summary>
public static class IntercomAttachResolveScopeKey
{
    public static string From(string? workspaceRoot, string? solutionPath)
    {
        var ws = (workspaceRoot ?? "").Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var sln = (solutionPath ?? "").Trim();
        if (ws.Length == 0 && sln.Length == 0)
            return "empty";

        var payload = ws + "\n" + sln;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}
