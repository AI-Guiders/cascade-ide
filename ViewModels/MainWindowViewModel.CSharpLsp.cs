using System.IO;
using System.Threading;
using Avalonia.Threading;
using CascadeIDE.Services.Lsp;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    private async Task RestartCSharpLanguageServerAsync()
    {
        var snap = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _workspaceDiagnostics.SetLspDiagnosticsHost(null);
            _csharpLspHost?.Dispose();
            _csharpLspHost = null;
            return (
                SolutionPath ?? "",
                _settings.CSharpLspProvider,
                _settings.CSharpLspExecutable,
                _settings.CSharpLspArguments);
        });

        if (string.Equals(snap.Item2, CSharpLspProviderIds.ParseOnly, StringComparison.OrdinalIgnoreCase))
            return;
        if (string.IsNullOrWhiteSpace(snap.Item1) || !File.Exists(snap.Item1))
            return;

        var host = new CSharpLspDiagnosticsHost();
        var ok = await host.TryStartAsync(snap.Item2, snap.Item1, snap.Item3, snap.Item4, CancellationToken.None)
            .ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!ok)
            {
                host.Dispose();
                return;
            }

            _csharpLspHost = host;
            _workspaceDiagnostics.SetLspDiagnosticsHost(host);
            foreach (var d in OpenDocuments)
            {
                if (d.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    host.EnsureOpened(d.FilePath, d.Content ?? "");
            }
        });
    }
}
