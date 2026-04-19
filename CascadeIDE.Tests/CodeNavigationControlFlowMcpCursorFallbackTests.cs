using System.Text.Json;
using Avalonia.Headless.XUnit;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationControlFlowMcpCursorFallbackTests
{
    [AvaloniaFact]
    public async Task GetCodeNavigationContext_ControlFlow_UsesCurrentCursorWhenLineColumnNotProvided()
    {
        var vm = new MainWindowViewModel
        {
            CurrentFilePath = @"D:\w\Demo.cs",
            EditorText = """
class Demo
{
    void A()
    {
        B();
    }

    void B() { }
}
"""
        };

        // Курсор внутри A(), на вызове B(); line/column в MCP-вызове не передаём.
        var offset = vm.EditorText.IndexOf("B();", StringComparison.Ordinal);
        Assert.True(offset >= 0);
        vm.EditorSelectionStart = offset;

        IIdeMcpActions mcp = vm;
        var json = await mcp.GetCodeNavigationContextAsync(
            mode: "related",
            filePath: null,
            line: null,
            column: null,
            maxRelated: null,
            maxNodes: null,
            maxEdges: null,
            preset: null,
            includeKinds: null,
            excludeKinds: null,
            level: "controlFlow");

        using var doc = JsonDocument.Parse(json);
        var nodes = doc.RootElement.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count > 1);
        Assert.Equal("method A", nodes[0].GetProperty("rationale").GetString());
    }
}
