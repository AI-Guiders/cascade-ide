#nullable enable

using System.IO.Pipes;
using System.Text;

namespace CascadeIDE.Features.MagicLink;

/// <summary>Mutex + named pipe для передачи URI второму экземпляру (ADR 0157 §3).</summary>
public static class CideMagicLinkSingleInstance
{
    public const string PipeName = "CascadeIDE.MagicLink.v1";

    private const string MutexName = @"Local\CascadeIDE.MagicLink.SingleInstance";

    public static bool TryAcquirePrimary(out IDisposable? release)
    {
        try
        {
            var mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out var createdNew);
            if (!createdNew)
            {
                mutex.Dispose();
                release = null;
                return false;
            }

            release = mutex;
            return true;
        }
        catch
        {
            release = null;
            return false;
        }
    }

    public static bool TryForwardToPrimary(string uri, int timeoutMs = 2500)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(timeoutMs);
            var bytes = Encoding.UTF8.GetBytes(uri);
            client.Write(bytes, 0, bytes.Length);
            client.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void RunPipeServerLoop(Func<string, Task> onUri, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        maxNumberOfServerInstances: 4,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                    using var ms = new MemoryStream();
                    var buffer = new byte[4096];
                    while (server.IsConnected)
                    {
                        var read = await server.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (read <= 0)
                            break;
                        ms.Write(buffer, 0, read);
                    }

                    var text = Encoding.UTF8.GetString(ms.ToArray()).Trim();
                    if (text.Length > 0)
                        await onUri(text).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                }
            }
        }, cancellationToken);
    }
}
