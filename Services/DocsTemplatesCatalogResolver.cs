using System.IO;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Services;

internal static class DocsTemplatesCatalogResolver
{
    public sealed record TemplateEntry(
        string Id,
        string Title,
        string Kind,
        string Source,
        string? RepoPath,
        string? KnowledgeRootId,
        string? KnowledgeFilePath);

    private sealed class DocsTemplatesCatalogRoot
    {
        public UiWorkspaceDocsTemplatesToml? DocsTemplates { get; set; }
    }

    public static IReadOnlyList<TemplateEntry> ResolveTemplatesFromWorkspaceToml(
        UiWorkspaceToml? workspaceToml,
        string workspaceRoot)
    {
        var list = new List<TemplateEntry>();

        // 1) catalog file
        var catalogPath = workspaceToml?.Workspace?.DocsTemplates?.CatalogPath;
        if (!string.IsNullOrWhiteSpace(catalogPath))
        {
            var abs = WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(workspaceRoot, catalogPath!);
            if (!string.IsNullOrWhiteSpace(abs) && File.Exists(abs))
            {
                try
                {
                    var raw = File.ReadAllText(abs);
                    var parsed = CascadeTomlSerializer.Deserialize<DocsTemplatesCatalogRoot>(raw);
                    if (parsed?.DocsTemplates?.Template is { Count: > 0 } templates)
                        Append(list, templates);
                }
                catch
                {
                    // ignore
                }
            }
        }

        // 2) inline
        if (workspaceToml?.Workspace?.DocsTemplates?.Template is { Count: > 0 } inline)
            Append(list, inline);

        // 3) fallback built-ins (repo)
        if (list.Count == 0)
        {
            list.Add(new TemplateEntry("feature.v1", "Feature doc", "feature_doc", "repo", "docs/templates/feature.md", null, null));
            list.Add(new TemplateEntry("module.v1", "Module doc", "module_doc", "repo", "docs/templates/module.md", null, null));
            list.Add(new TemplateEntry("adr-mini.v1", "ADR mini", "adr", "repo", "docs/templates/adr-mini.md", null, null));
            list.Add(new TemplateEntry("runbook.v1", "Runbook", "runbook", "repo", "docs/templates/runbook.md", null, null));
        }

        // De-dup by id (later wins).
        var dict = new Dictionary<string, TemplateEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in list)
            dict[t.Id] = t;

        return dict.Values.OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
    }

    private static void Append(List<TemplateEntry> dst, List<DocsTemplateToml> templates)
    {
        foreach (var t in templates)
        {
            var id = (t.Id ?? "").Trim();
            if (id.Length == 0)
                continue;

            var title = (t.Title ?? id).Trim();
            var kind = (t.Kind ?? "").Trim();
            var source = (t.Source ?? "repo").Trim().ToLowerInvariant();

            if (source == "knowledge")
            {
                var rootId = (t.KnowledgeRootId ?? "").Trim();
                var file = (t.FilePath ?? "").Trim();
                if (file.Length == 0)
                    continue;

                dst.Add(new TemplateEntry(
                    id,
                    title,
                    kind,
                    "knowledge",
                    null,
                    rootId.Length == 0 ? null : rootId,
                    file));
                continue;
            }

            var path = (t.Path ?? "").Trim();
            if (path.Length == 0)
                continue;

            dst.Add(new TemplateEntry(id, title, kind, "repo", path, null, null));
        }
    }
}

