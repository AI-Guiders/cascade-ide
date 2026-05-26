namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Пути .cs для L0 из того же набора, что прогревает solution warm-up (ADR 0141 → 0148 L0).</summary>
internal static class AgentL0WarmupPathCollector
{
    public static IReadOnlyList<string> Collect(
        bool enabled,
        bool warmActiveFile,
        bool warmOpenDocuments,
        bool warmRecentCsFiles,
        int maxOpenDocumentFiles,
        Func<IReadOnlyList<string>> getOpenCsPaths,
        Func<string?> getActiveCsPath)
    {
        if (!enabled)
            return [];

        if (!warmActiveFile && !warmOpenDocuments && !warmRecentCsFiles)
            return [];

        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return;

            string full;
            try
            {
                full = Path.GetFullPath(path.Trim());
            }
            catch
            {
                return;
            }

            if (!File.Exists(full))
                return;
            if (!seen.Add(full))
                return;

            ordered.Add(full);
        }

        if (warmActiveFile)
            Add(getActiveCsPath());

        if (warmOpenDocuments || warmRecentCsFiles)
        {
            foreach (var path in getOpenCsPaths())
                Add(path);
        }

        var max = Math.Clamp(maxOpenDocumentFiles, 1, 32);
        return ordered.Count <= max ? ordered : ordered.Take(max).ToList();
    }
}
