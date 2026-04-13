using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CascadeIDE.Services.Lsp;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>Варианты Markdown LSP (настройки).</summary>
    public IReadOnlyList<string> MarkdownLspProviderOptionsList => MarkdownLspProviderIds.All;

    public bool IsMarkdownLspProcessSelected =>
        !string.Equals(_markdownLspProvider, MarkdownLspProviderIds.Off, StringComparison.OrdinalIgnoreCase);

    private string _markdownLspProvider = MarkdownLspProviderIds.Off;
    private string _markdownLspExecutable = "";
    private string _markdownLspArguments = "";

    /// <summary><see cref="MarkdownLspProviderIds"/>.</summary>
    public string MarkdownLspProvider
    {
        get => _markdownLspProvider;
        set
        {
            var v = string.IsNullOrWhiteSpace(value) ? MarkdownLspProviderIds.Off : value.Trim();
            if (!SetProperty(ref _markdownLspProvider, v))
                return;
            _settings.MarkdownLsp.Provider = v;
            SaveSettingsIfChanged();
            OnPropertyChanged(nameof(IsMarkdownLspProcessSelected));
            _ = RestartMarkdownLanguageServerAsync();
        }
    }

    public string MarkdownLspExecutable
    {
        get => _markdownLspExecutable;
        set
        {
            var v = value ?? "";
            if (!SetProperty(ref _markdownLspExecutable, v))
                return;
            _settings.MarkdownLsp.Executable = v;
            SaveSettingsIfChanged();
            _ = RestartMarkdownLanguageServerAsync();
        }
    }

    public string MarkdownLspArguments
    {
        get => _markdownLspArguments;
        set
        {
            var v = value ?? "";
            if (!SetProperty(ref _markdownLspArguments, v))
                return;
            _settings.MarkdownLsp.Arguments = v;
            SaveSettingsIfChanged();
            _ = RestartMarkdownLanguageServerAsync();
        }
    }

    private async Task RestartMarkdownLanguageServerAsync()
    {
        var snap = await UiScheduler.Default.InvokeAsync(() =>
        {
            _workspaceDiagnostics.SetMarkdownLspDiagnosticsHost(null);
            _markdownLspHost?.Dispose();
            _markdownLspHost = null;
            return (
                Workspace.SolutionPath ?? "",
                _settings.MarkdownLsp.Provider,
                _settings.MarkdownLsp.Executable,
                _settings.MarkdownLsp.Arguments);
        });

        if (string.Equals(snap.Item2, MarkdownLspProviderIds.Off, StringComparison.OrdinalIgnoreCase))
            return;
        if (string.IsNullOrWhiteSpace(snap.Item1) || !File.Exists(snap.Item1))
            return;

        var host = new MarkdownLspDiagnosticsHost();
        var ok = await host.TryStartAsync(snap.Item2, snap.Item1, snap.Item3, snap.Item4, CancellationToken.None)
            .ConfigureAwait(false);

        await UiScheduler.Default.InvokeAsync(() =>
        {
            if (!ok)
            {
                host.Dispose();
                return;
            }

            _markdownLspHost = host;
            _workspaceDiagnostics.SetMarkdownLspDiagnosticsHost(host);
            foreach (var d in Documents.OpenDocuments)
            {
                if (d.FilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    || d.FilePath.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase))
                    host.EnsureOpened(d.FilePath, d.Content ?? "");
            }
        });
    }
}
