using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    /// <summary>Format entire document (Roslyn in-proc, ADR 0148).</summary>
    public string FormatDocument(string filePath, string sourceText, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath))
            return sourceText;

        try
        {
            var text = SourceText.From(sourceText);
            var (compilation, tree) = GetOrCreateCompilationAndTree(filePath, text, ct);
            var root = tree.GetRoot(ct);

            using var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "FormatTemp",
                "FormatTemp",
                LanguageNames.CSharp,
                metadataReferences: compilation.References);
            workspace.AddProject(projectInfo);
            workspace.AddDocument(DocumentInfo.Create(
                documentId,
                Path.GetFileName(filePath),
                loader: TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())),
                filePath: filePath));

            var formatted = Formatter.Format(root, workspace, cancellationToken: ct);
            return formatted.ToFullString();
        }
        catch
        {
            return sourceText;
        }
    }
}
