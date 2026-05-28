#nullable enable

using CascadeIDE.Features.Workspace;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary><c>[[workspace.correspondence.code_anchors]]</c> → explicit reverse entries.</summary>
public static class WorkspaceCorrespondenceCodeAnchorsLoader
{
    public const string DefaultKind = "documents";

    public static IReadOnlyList<WorkspaceExplicitCodeAnchor> LoadFromWorkspaceToml(
        RepositoryWorkspaceToml? workspaceToml,
        string workspaceRoot)
    {
        var entries = workspaceToml?.Workspace?.Correspondence?.CodeAnchors;
        if (entries is not { Count: > 0 })
            return [];

        var root = workspaceRoot.Trim();
        var list = new List<WorkspaceExplicitCodeAnchor>();

        foreach (var row in entries)
        {
            if (!TryToExplicit(row, out var explicitAnchor))
                continue;

            list.Add(explicitAnchor with { DocPath = NormalizeDoc(explicitAnchor.DocPath) });
        }

        return list;
    }

    private static bool TryToExplicit(RepositoryCorrespondenceCodeAnchorToml row, out WorkspaceExplicitCodeAnchor explicitAnchor)
    {
        explicitAnchor = new WorkspaceExplicitCodeAnchor("", new CodeAnchor(""), DefaultKind, DocReverseAnchorResolver.ProvenanceWorkspaceToml);

        var doc = (row.Doc ?? "").Trim();
        if (doc.Length == 0)
            return false;

        var kind = string.IsNullOrWhiteSpace(row.Kind) ? DefaultKind : row.Kind.Trim();

        if (!string.IsNullOrWhiteSpace(row.Bracket))
        {
            if (!BracketCodeReferenceParser.TryParse(row.Bracket, out var reference, out _)
                || string.IsNullOrWhiteSpace(reference.File))
                return false;

            explicitAnchor = new WorkspaceExplicitCodeAnchor(
                doc,
                new CodeAnchor(
                    reference.File!.Replace('\\', '/'),
                    reference.LineStart ?? row.LineStart,
                    reference.LineEnd ?? row.LineEnd,
                    reference.MemberKey ?? row.MemberKey,
                    reference.ScopeKind is null ? null : $"{reference.ScopeKind}:{reference.ScopeIndexInParent}"),
                kind,
                DocReverseAnchorResolver.ProvenanceWorkspaceToml);
            return true;
        }

        var file = (row.File ?? "").Trim();
        if (file.Length == 0)
            return false;

        explicitAnchor = new WorkspaceExplicitCodeAnchor(
            doc,
            new CodeAnchor(
                file.Replace('\\', '/'),
                row.LineStart,
                row.LineEnd,
                string.IsNullOrWhiteSpace(row.MemberKey) ? null : row.MemberKey.Trim()),
            kind,
            DocReverseAnchorResolver.ProvenanceWorkspaceToml);
        return true;
    }

    private static string NormalizeDoc(string doc) =>
        doc.Replace('\\', '/').Trim().TrimStart('/');
}
