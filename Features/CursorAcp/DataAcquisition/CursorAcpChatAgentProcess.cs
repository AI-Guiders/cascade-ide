#nullable enable
using System.Diagnostics;
using System.Text;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.CursorAcp.DataAcquisition;

/// <summary>
/// DAL: stdio-процесс <c>cursor-agent acp</c> для <see cref="CascadeIDE.Services.CursorAcp.CursorAcpChatConnection"/> (UTF-8, перенаправленные потоки).
/// </summary>
[IoBoundary]
public static class CursorAcpChatAgentProcess
{
    /// <summary>
    /// Создаёт процесс, подписывается на stderr (построчно), вызывает <see cref="Process.Start"/>, затем <see cref="Process.BeginErrorReadLine"/>.
    /// </summary>
    /// <param name="cmdPath">Полный путь к <c>cursor-agent.cmd</c> или аналогу.</param>
    /// <param name="agentWorkingDirectory">Рабочий каталог агента; если пусто — каталог <paramref name="cmdPath"/>.</param>
    /// <param name="onStderrLine">Не-null строки из stderr (как в <c>ErrorDataReceived</c>).</param>
    /// <exception cref="InvalidOperationException">Не удалось запустить процесс.</exception>
    public static Process Start(string cmdPath, string agentWorkingDirectory, Action<string>? onStderrLine)
    {
        var workDir = string.IsNullOrEmpty(agentWorkingDirectory)
            ? Path.GetDirectoryName(cmdPath) ?? ""
            : agentWorkingDirectory;

        var psi = new ProcessStartInfo
        {
            FileName = cmdPath,
            Arguments = "acp",
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // Иначе на Windows OEM — JSON/UTF-8 от агента искажается.
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        var p = new Process { StartInfo = psi };
        if (onStderrLine is not null)
        {
            p.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    onStderrLine(e.Data);
            };
        }

        if (!p.Start())
            throw new InvalidOperationException("Не удалось запустить cursor-agent (ACP).");

        p.BeginErrorReadLine();
        return p;
    }
}
