namespace CascadeIDE.ViewModels;

/// <summary>Ctor-time diagnose-files helpers for agent environment warmup.</summary>
public partial class MainWindowViewModel
{
    private IReadOnlyList<(string Path, string Content)> GetOpenCsDocumentsForDiagnoseFiles()
    {
        var list = new List<(string Path, string Content)>();
        foreach (var doc in Documents.OpenDocuments)
        {
            if (!doc.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;
            list.Add((doc.FilePath, doc.Content ?? ""));
        }

        return list;
    }

    private IReadOnlyList<string> GetDiagnoseFilesWarmupCsFilePaths()
    {
        var warmup = _settings.SolutionWarmup;
        return Features.Agent.Environment.AgentDiagnoseFilesWarmupPathCollector.Collect(
            warmup.Enabled,
            warmup.WarmActiveFileOnSolutionOpen,
            warmup.WarmOpenDocuments,
            warmup.WarmRecentCsFiles,
            warmup.MaxOpenDocumentFiles,
            () => Documents.OpenDocuments
                .Select(d => d.FilePath)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList(),
            () => CurrentFilePath);
    }
}
