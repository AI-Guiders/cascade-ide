using System.Diagnostics;
using System.Text.Json;
using AgentClientProtocol;

// Локальный smoke: .NET-клиент ACP поднимает тот же Python echo_agent, что и samples/AcpSmoke.
// Зависимость: Python + pip install agent-client-protocol (для echo_agent.py).

var echoAgentPath = ResolveEchoAgentPath(args);
var python = Environment.GetEnvironmentVariable("ACP_PYTHON") ?? "python";

using var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = python,
        Arguments = $"\"{echoAgentPath}\"",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardInput = true,
        RedirectStandardError = true,
        WorkingDirectory = Path.GetDirectoryName(echoAgentPath)!,
    },
};

process.ErrorDataReceived += (_, e) =>
{
    if (!string.IsNullOrEmpty(e.Data))
        Console.Error.WriteLine($"[agent stderr] {e.Data}");
};

try
{
    if (!process.Start())
        throw new InvalidOperationException("Не удалось запустить процесс агента.");

    process.BeginErrorReadLine();

    var client = new SmokeClient();
    using var connection = new ClientSideConnection(_ => client, process.StandardOutput, process.StandardInput);
    connection.Open();

    var initResult = await connection.InitializeAsync(new InitializeRequest
    {
        ProtocolVersion = 1,
        ClientInfo = new Implementation { Name = "cascade-acp-smoke-dotnet", Version = "0.1.0" },
        ClientCapabilities = new ClientCapabilities
        {
            Fs = new FileSystemCapability { ReadTextFile = true, WriteTextFile = true },
        },
    });

    Console.WriteLine($"initialize: protocol v{initResult.ProtocolVersion}");

    var sessionRoot = Path.GetDirectoryName(echoAgentPath)!;
    var sessionResult = await connection.NewSessionAsync(new NewSessionRequest
    {
        Cwd = sessionRoot,
        McpServers = [],
    });

    Console.WriteLine($"session: {sessionResult.SessionId}");

    var promptResult = await connection.PromptAsync(new PromptRequest
    {
        SessionId = sessionResult.SessionId,
        Prompt = [new TextContentBlock { Text = "Hello ACP from .NET AcpSmokeDotnet" }],
    });

    Console.WriteLine($"prompt stop_reason: {promptResult.StopReason}");
    Console.WriteLine("ACP smoke (.NET) OK");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[Client] {ex}");
    Environment.ExitCode = 1;
}
finally
{
    try
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(milliseconds: 5000);
        }
    }
    catch
    {
        // ignore
    }
}

static string ResolveEchoAgentPath(string[] args)
{
    if (args.Length > 0 && File.Exists(args[0]))
        return Path.GetFullPath(args[0]);

    var env = Environment.GetEnvironmentVariable("ACP_ECHO_AGENT_PATH");
    if (!string.IsNullOrEmpty(env) && File.Exists(env))
        return Path.GetFullPath(env);

    foreach (var root in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var dir = new DirectoryInfo(root);
        while (dir != null)
        {
            var p = Path.Combine(dir.FullName, "samples", "AcpSmoke", "echo_agent.py");
            if (File.Exists(p))
                return Path.GetFullPath(p);
            dir = dir.Parent;
        }
    }

    throw new FileNotFoundException(
        "Не найден echo_agent.py. Укажи путь первым аргументом или установи ACP_ECHO_AGENT_PATH.");
}

/// <summary>Минимальная реализация клиента: эхо в консоль, права — отмена (как в Python smoke).</summary>
internal sealed class SmokeClient : IAcpClient
{
    public ValueTask<RequestPermissionResponse> RequestPermissionAsync(
        RequestPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new RequestPermissionResponse
        {
            Outcome = new CancelledRequestPermissionOutcome(),
        });
    }

    public ValueTask SessionNotificationAsync(SessionNotification notification, CancellationToken cancellationToken = default)
    {
        var update = notification.Update;
        if (update is AgentMessageChunkSessionUpdate chunk && chunk.Content is TextContentBlock text)
            Console.WriteLine($"session_update: {text.Text}");
        return ValueTask.CompletedTask;
    }

    public ValueTask<WriteTextFileResponse> WriteTextFileAsync(WriteTextFileRequest request, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new WriteTextFileResponse());

    public ValueTask<ReadTextFileResponse> ReadTextFileAsync(ReadTextFileRequest request, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new ReadTextFileResponse { Content = "" });

    public ValueTask<CreateTerminalResponse> CreateTerminalAsync(CreateTerminalRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public ValueTask<TerminalOutputResponse> TerminalOutputAsync(TerminalOutputRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public ValueTask<ReleaseTerminalResponse> ReleaseTerminalAsync(ReleaseTerminalRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public ValueTask<WaitForTerminalExitResponse> WaitForTerminalExitAsync(WaitForTerminalExitRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public ValueTask<KillTerminalCommandResponse> KillTerminalCommandAsync(KillTerminalCommandRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public ValueTask<JsonElement> ExtMethodAsync(string method, JsonElement request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public ValueTask ExtNotificationAsync(string method, JsonElement notification, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
