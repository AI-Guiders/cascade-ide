using System.Diagnostics;
using System.Text;

namespace CascadeIDE.Features.Build.DataAcquisition;

/// <summary>Кодировка stdout/stderr для <c>dotnet</c> CLI (согласовано с LSP/ACP хостами и dotnet-build-test-mcp).</summary>
internal static class DotnetProcessIoEncoding
{
    internal static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    internal static void ApplyUtf8(ProcessStartInfo psi)
    {
        ArgumentNullException.ThrowIfNull(psi);
        psi.StandardOutputEncoding = Utf8NoBom;
        psi.StandardErrorEncoding = Utf8NoBom;
    }
}
