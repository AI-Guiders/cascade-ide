using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceCorrespondenceCodeAnchorsLoaderTests
{
    [Fact]
    public void Toml_deserializes_code_anchors_array()
    {
        const string toml = """
            [[workspace.correspondence.code_anchors]]
            doc = "docs/adr/0099-sample.md"
            file = "src/Foo.cs"
            line_start = 10
            kind = "documents"
            """;

        var model = CascadeTomlSerializer.Deserialize<RepositoryWorkspaceToml>(toml);
        Assert.NotNull(model?.Workspace?.Correspondence?.CodeAnchors);
        Assert.Single(model!.Workspace!.Correspondence!.CodeAnchors);
        Assert.Equal("docs/adr/0099-sample.md", model.Workspace.Correspondence.CodeAnchors[0].Doc);
        Assert.Equal(10, model.Workspace.Correspondence.CodeAnchors[0].LineStart);
    }

    [Fact]
    public void Explicit_anchor_wins_over_doc_body_scan()
    {
        var root = Path.Combine(Path.GetTempPath(), "crs-toml-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var anchorAbs = Path.Combine(root, "src", "Foo.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(anchorAbs)!);
            File.WriteAllText(anchorAbs, "//");

            const string docRel = "docs/adr/x.md";
            var docAbs = Path.Combine(root, docRel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(docAbs)!);
            File.WriteAllText(docAbs, "Also `[F:src/Foo.cs]` in prose.");

            var toml = new RepositoryWorkspaceToml
            {
                Workspace = new RepositoryWorkspaceSectionToml
                {
                    Correspondence = new RepositoryCorrespondenceToml
                    {
                        CodeAnchors =
                        [
                            new RepositoryCorrespondenceCodeAnchorToml
                            {
                                Doc = docRel,
                                File = "src/Foo.cs",
                                LineStart = 5,
                                Kind = "documents"
                            }
                        ]
                    }
                }
            };

            var explicitAnchors = WorkspaceCorrespondenceCodeAnchorsLoader.LoadFromWorkspaceToml(toml, root);
            var hits = DocReverseAnchorResolver.Resolve(root, anchorAbs, [docRel], explicitAnchors);

            Assert.Single(hits);
            Assert.Equal(DocReverseAnchorResolver.ProvenanceWorkspaceToml, hits[0].Provenance);
            Assert.Equal(5, hits[0].CodeAnchor.LineStart);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
