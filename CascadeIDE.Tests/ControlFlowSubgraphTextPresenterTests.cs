using Xunit;

namespace CascadeIDE.Tests;

public sealed class ControlFlowSubgraphTextPresenterTests
{
    [Fact]
    public void Render_LinearGraph_ReturnsSingleLineChain()
    {
        const string json = """
{
  "mode": "subgraph",
  "anchor_path": "D:\\w\\Demo.cs",
  "nodes": [
    { "id": "n0", "kind": "anchor", "label": "Demo.cs", "path": "D:\\w\\Demo.cs", "relative_path": "", "rationale": "method A" },
    { "id": "n1", "kind": "call_step", "label": "B", "path": "D:\\w\\Demo.cs", "relative_path": "", "rationale": "call B" },
    { "id": "n2", "kind": "call_step", "label": "C", "path": "D:\\w\\Demo.cs", "relative_path": "", "rationale": "call C" }
  ],
  "edges": [
    { "from_id": "n0", "to_id": "n1", "kind": "Call", "related_kind": "Call" },
    { "from_id": "n1", "to_id": "n2", "kind": "Call", "related_kind": "Call" }
  ]
}
""";

        var text = ControlFlowSubgraphTextPresenter.Render(json);
        Assert.Equal("A -(Call)-> B -(Call)-> C", text);
    }

    [Fact]
    public void Render_BranchedGraph_UsesTreeShape()
    {
        const string json = """
{
  "mode": "subgraph",
  "anchor_path": "D:\\w\\Demo.cs",
  "nodes": [
    { "id": "n0", "kind": "anchor", "label": "Demo.cs", "path": "D:\\w\\Demo.cs", "relative_path": "", "rationale": "method A" },
    { "id": "n1", "kind": "condition_step", "label": "IF", "path": "D:\\w\\Demo.cs", "relative_path": "", "rationale": "if condition" },
    { "id": "n2", "kind": "exit_step", "label": "RET", "path": "D:\\w\\Demo.cs", "relative_path": "", "rationale": "return" },
    { "id": "n3", "kind": "call_step", "label": "DoWork", "path": "D:\\w\\Demo.cs", "relative_path": "", "rationale": "call DoWork" }
  ],
  "edges": [
    { "from_id": "n0", "to_id": "n1", "kind": "ConditionalCall", "related_kind": "ConditionalCall" },
    { "from_id": "n1", "to_id": "n2", "kind": "Exit", "related_kind": "Exit" },
    { "from_id": "n1", "to_id": "n3", "kind": "Merge", "related_kind": "Call" }
  ]
}
""";

        var text = ControlFlowSubgraphTextPresenter.Render(json);
        Assert.Contains("A", text, StringComparison.Ordinal);
        Assert.Contains("|-- (Exit) R", text, StringComparison.Ordinal);
        Assert.Contains("`-- (Merge) DoWork", text, StringComparison.Ordinal);
    }
}
