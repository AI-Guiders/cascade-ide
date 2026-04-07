using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CascadeIDE.Services.Lsp;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>Варианты C# LSP (настройки; активен не более одного процесса).</summary>
    public IReadOnlyList<string> CSharpLspProviderOptionsList => CSharpLspProviderIds.All;

    public bool IsCSharpLspProcessSelected =>
        !string.Equals(_csharpLspProvider, CSharpLspProviderIds.ParseOnly, StringComparison.OrdinalIgnoreCase);

    private string _csharpLspProvider = CSharpLspProviderIds.ParseOnly;
    private string _csharpLspExecutable = "";
    private string _csharpLspArguments = "";

    /// <summary><see cref="CSharpLspProviderIds"/>.</summary>
    public string CSharpLspProvider
    {
        get => _csharpLspProvider;
        set
        {
            var v = string.IsNullOrWhiteSpace(value) ? CSharpLspProviderIds.ParseOnly : value.Trim();
            if (!SetProperty(ref _csharpLspProvider, v))
                return;
            _settings.CSharpLspProvider = v;
            SaveSettingsIfChanged();
            OnPropertyChanged(nameof(IsCSharpLspProcessSelected));
            _ = RestartCSharpLanguageServerAsync();
        }
    }

    public string CSharpLspExecutable
    {
        get => _csharpLspExecutable;
        set
        {
            var v = value ?? "";
            if (!SetProperty(ref _csharpLspExecutable, v))
                return;
            _settings.CSharpLspExecutable = v;
            SaveSettingsIfChanged();
            _ = RestartCSharpLanguageServerAsync();
        }
    }

    public string CSharpLspArguments
    {
        get => _csharpLspArguments;
        set
        {
            var v = value ?? "";
            if (!SetProperty(ref _csharpLspArguments, v))
                return;
            _settings.CSharpLspArguments = v;
            SaveSettingsIfChanged();
            _ = RestartCSharpLanguageServerAsync();
        }
    }

    private async Task RestartCSharpLanguageServerAsync()
    {
        var snap = await UiScheduler.Default.InvokeAsync(() =>
        {
            _workspaceDiagnostics.SetLspDiagnosticsHost(null);
            _csharpLspHost?.Dispose();
            _csharpLspHost = null;
            return (
                Workspace.SolutionPath ?? "",
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

        await UiScheduler.Default.InvokeAsync(() =>
        {
            if (!ok)
            {
                host.Dispose();
                return;
            }

            _csharpLspHost = host;
            _workspaceDiagnostics.SetLspDiagnosticsHost(host);
            foreach (var d in Documents.OpenDocuments)
            {
                if (d.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    host.EnsureOpened(d.FilePath, d.Content ?? "");
            }
        });
    }

    /// <summary>Текст Quick Info для тултипа: при активном C# LSP — <c>textDocument/hover</c>, иначе in-process Roslyn.</summary>
    public async Task<string?> GetEditorQuickInfoAsync(string filePath, string sourceText, int line, int column, CancellationToken ct = default)
    {
        if (_csharpLspHost is { IsActive: true })
        {
            try
            {
                var h = await _csharpLspHost.RequestHoverAsync(filePath, sourceText, line, column, ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(h))
                    return h.Trim();
            }
            catch
            {
                // fallback: Roslyn
            }
        }

        return CSharpLanguage.GetQuickInfo(filePath, sourceText, line, column, ct);
    }
}
