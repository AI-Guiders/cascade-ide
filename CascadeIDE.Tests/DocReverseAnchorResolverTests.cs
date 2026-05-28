using CascadeIDE.Features.WorkspaceNavigation.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class DocReverseAnchorResolverTests
{
    [Fact]
    public void Resolve_finds_bracket_anchor_in_adr_body()
    {
        var root = Path.Combine(Path.GetTempPath(), "crs-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var anchorAbs = Path.Combine(root, "src", "Foo.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(anchorAbs)!);
            File.WriteAllText(anchorAbs, "// code");

            const string docRel = "docs/adr/sample.md";
            var docAbs = Path.Combine(root, docRel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(docAbs)!);
            File.WriteAllText(
                docAbs,
                "Implementation: [F:src/Foo.cs M:RunAsync].");

            var hits = DocReverseAnchorResolver.Resolve(root, anchorAbs, [docRel]);

            Assert.Single(hits);
            Assert.Equal(DocReverseAnchorResolver.ProvenanceBracket, hits[0].Provenance);
            Assert.Equal("RunAsync", hits[0].CodeAnchor.MemberKey);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
