using CasaField.Core;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CasaFieldCoreTests
{
    [Fact]
    public void GridDecoder_VotesConceptTokens()
    {
        var cells = new List<IReadOnlyList<string>>
        {
            new[] { "C:casa.lab.import_knowledge_delta", "C:casa.lab.import_knowledge_delta" },
            new[] { "C:casa.lab.import_knowledge_delta" },
            new List<string>(),
        };

        var decoded = GridDecoder.Decode(cells, minVotes: 2);
        Assert.Contains("casa.lab.import_knowledge_delta", decoded.ConceptIds);
    }

    [Fact]
    public void HotPath_LoadsResearchAgentStore()
    {
        var store = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "..", "casa-ontology-payload", "examples", "agent-stores", "research-agent-lab-v0"));

        if (!File.Exists(Path.Combine(store, "field_state.json")))
            return;

        var result = CasaFieldHotPath.Run(store, "import knowledge delta");
        Assert.True(result.WallMs >= 0);
        Assert.NotEmpty(result.ClaimsNav);
    }
}
