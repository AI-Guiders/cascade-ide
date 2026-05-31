namespace CascadeIDE.Features.Terminal.DataAcquisition;

internal interface IIntegratedShellSession : IDisposable
{
    event Action<byte[]>? DataReceived;

    event Action<int>? Exited;

    void Start();

    void Send(byte[] input);

    void Resize(int cols, int rows);
}

internal sealed record ShellLaunchConfiguration(
    string FileName,
    IReadOnlyList<string> Arguments,
    string DisplayName,
    string WorkingDirectory);
