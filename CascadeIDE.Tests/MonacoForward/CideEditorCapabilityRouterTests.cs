#nullable enable

using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Services;
using Microsoft.CodeAnalysis;
using Xunit;
using TestContext = Xunit.TestContext;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class CideEditorCapabilityRouterTests
{
    private readonly CideEditorCapabilityRouter _router = new();

    [Fact]
    public void CanHandle_capability_requests_and_codeLensClick()
    {
        Assert.True(_router.CanHandle(Inbound(CideEditorBusManifest.Capabilities.Completion, 1, 1, 1)));
        Assert.True(_router.CanHandle(Inbound(CideEditorBusManifest.Capabilities.CodeLensClick, null, null, null)));
        Assert.False(_router.CanHandle(Inbound(CideEditorBusManifest.Editor.DidChange, null, null, null)));
    }

    [Fact]
    public async Task Completion_nonCs_returns_empty()
    {
        var host = new RecordingCapabilityHost();
        var ctx = CreateContext(host, filePath: @"D:\x\readme.txt", text: "hello");

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.Completion, requestId: 1, line: 1, column: 1),
            ctx,
            TestContext.Current.CancellationToken);

        var result = host.Completions.Single();
        Assert.Equal(1, result.RequestId);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Completion_cs_uses_roslyn_when_lsp_inactive()
    {
        var host = new RecordingCapabilityHost();
        const string path = @"D:\Fake\Complete.cs";
        var text = """
            namespace N;
            public class C
            {
                public void M() { }
            }
            """;
        var ctx = CreateContext(host, path, text);

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.Completion, requestId: 7, line: 4, column: 17),
            ctx,
            TestContext.Current.CancellationToken);

        var result = host.Completions.Single();
        Assert.Equal(7, result.RequestId);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task Completion_prefers_lsp_when_active_and_non_empty()
    {
        var host = new RecordingCapabilityHost();
        var lsp = new FakeLspIntelligence
        {
            CompletionItems =
            [
                new CideEditorCompletionItem("LspItem", "lsp", "from lsp"),
            ],
        };
        var ctx = CreateContext(host, @"D:\Fake\Lsp.cs", "class X { }", lsp);

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.Completion, requestId: 2, line: 1, column: 8),
            ctx,
            TestContext.Current.CancellationToken);

        var result = host.Completions.Single();
        Assert.Equal("LspItem", result.Items[0].Label);
        Assert.Equal(1, lsp.CompletionCalls);
    }

    [Fact]
    public async Task Completion_falls_back_to_roslyn_when_lsp_returns_empty()
    {
        var host = new RecordingCapabilityHost();
        var lsp = new FakeLspIntelligence { CompletionItems = [] };
        const string path = @"D:\Fake\Fallback.cs";
        var text = "class X { public void M() { } }";
        var ctx = CreateContext(host, path, text, lsp);

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.Completion, requestId: 3, line: 1, column: 20),
            ctx,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, lsp.CompletionCalls);
        Assert.NotEmpty(host.Completions.Single().Items);
    }

    [Fact]
    public async Task Hover_uses_quick_info_when_no_diagnostic_hit()
    {
        var host = new RecordingCapabilityHost();
        var ctx = new MonacoEditorCapabilityContext
        {
            Host = host,
            FilePath = @"D:\Fake\Hover.cs",
            GetEditorText = () => "class X { }",
            CSharpLanguage = new CSharpLanguageService(),
            WorkspaceDiagnostics = CreateDiagnostics(),
            ResolveQuickInfoAsync = (_, _, _, _, _) => Task.FromResult<string?>("**info**: ok"),
        };

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.Hover, requestId: 4, line: 1, column: 8),
            ctx,
            TestContext.Current.CancellationToken);

        Assert.Equal("**info**: ok", host.Hovers.Single().Markdown);
    }

    [Fact]
    public void HitTestForToolTip_finds_diagnostic_on_line()
    {
        var strips = new List<EditorDiagnosticStrip>
        {
            new(0, 3, DiagnosticSeverity.Warning, "CS1234", "bad token", Line1: 1, Column1: 1),
        };
        var hit = WorkspaceDiagnosticsCoordinator.HitTestForToolTip(strips, offset: 1, line1: 1, col1: 2, documentText: "bad\n");
        Assert.NotNull(hit);
        Assert.Equal("CS1234", hit!.Id);
    }

    [Fact]
    public async Task Signature_without_paren_returns_null()
    {
        var host = new RecordingCapabilityHost();
        var ctx = CreateContext(host, @"D:\Fake\Sig.cs", "class X { }");

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.SignatureHelp, requestId: 5, line: 1, column: 8),
            ctx,
            TestContext.Current.CancellationToken);

        Assert.Null(host.Signatures.Single().Signature);
    }

    [Fact]
    public async Task Definition_cs_returns_location()
    {
        var host = new RecordingCapabilityHost();
        const string path = @"D:\Fake\Def.cs";
        var text = "class X { public void M() { M(); } }";
        var ctx = CreateContext(host, path, text);

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.Definition, requestId: 6, line: 1, column: 30),
            ctx,
            TestContext.Current.CancellationToken);

        var def = host.Definitions.Single().Location;
        Assert.NotNull(def);
        Assert.True(def!.Line >= 1);
    }

    [Fact]
    public async Task CodeLensClick_invokes_navigate_delegate()
    {
        var host = new RecordingCapabilityHost();
        string? clicked = null;
        var ctx = new MonacoEditorCapabilityContext
        {
            Host = host,
            FilePath = @"D:\Fake\Lens.cs",
            GetEditorText = () => "class X { }",
            CSharpLanguage = new CSharpLanguageService(),
            WorkspaceDiagnostics = CreateDiagnostics(),
            ResolveQuickInfoAsync = (_, _, _, _, _) => Task.FromResult<string?>(null),
            TryNavigateCodeLens = id =>
            {
                clicked = id;
                return true;
            },
        };

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.CodeLensClick, lensId: "lens-1"),
            ctx,
            TestContext.Current.CancellationToken);

        Assert.Equal("lens-1", clicked);
        Assert.Empty(host.Completions);
    }

    [Fact]
    public async Task SemanticTokens_without_lsp_returns_empty()
    {
        var host = new RecordingCapabilityHost();
        var ctx = CreateContext(host, @"D:\Fake\Tok.cs", "class X { }");

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.SemanticTokens, requestId: 8, line: 1, column: 1),
            ctx,
            TestContext.Current.CancellationToken);

        Assert.Empty(host.SemanticTokens.Single().Data);
    }

    [Fact]
    public async Task Format_returns_roslyn_formatted_text()
    {
        var host = new RecordingCapabilityHost();
        var ctx = CreateContext(host, @"D:\Fake\Fmt.cs", "class C{void M(){}}");

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.Format, requestId: 10, line: 1, column: 1),
            ctx,
            TestContext.Current.CancellationToken);

        var formatted = host.Formats.Single().Text;
        Assert.False(string.IsNullOrWhiteSpace(formatted));
        Assert.NotEqual("class C{void M(){}}", formatted);
    }

    [Fact]
    public async Task References_falls_back_to_roslyn_in_file()
    {
        var host = new RecordingCapabilityHost();
        const string path = @"D:\Fake\Refs.cs";
        var text = """
            class C { void M() { int count = 1; count = count + 1; } }
            """;
        var markerIndex = text.IndexOf("count + 1", StringComparison.Ordinal);
        var pos = Microsoft.CodeAnalysis.Text.SourceText.From(text).Lines.GetLinePosition(markerIndex);
        var ctx = CreateContext(host, path, text);

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.References, requestId: 11, line: pos.Line + 1, column: pos.Character + 1),
            ctx,
            TestContext.Current.CancellationToken);

        Assert.True(host.References.Single().Locations.Count >= 2);
    }

    [Fact]
    public async Task Definition_cross_file_invokes_navigate_and_null_result()
    {
        var host = new RecordingCapabilityHost();
        CideEditorDefinitionLocation? navigated = null;
        var lsp = new FakeLspIntelligence
        {
            Definition = new CideEditorDefinitionLocation(@"D:\Fake\Other.cs", 3, 5),
        };
        var ctx = new MonacoEditorCapabilityContext
        {
            Host = host,
            FilePath = @"D:\Fake\Current.cs",
            GetEditorText = () => "class A { void M() { Other(); } }",
            CSharpLanguage = new CSharpLanguageService(),
            WorkspaceDiagnostics = CreateDiagnostics(),
            ResolveQuickInfoAsync = (_, _, _, _, _) => Task.FromResult<string?>(null),
            CSharpLspHost = lsp,
            NavigateToLocationAsync = loc =>
            {
                navigated = loc;
                return Task.CompletedTask;
            },
        };

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Capabilities.Definition, requestId: 12, line: 1, column: 20),
            ctx,
            TestContext.Current.CancellationToken);

        Assert.NotNull(navigated);
        Assert.Equal(@"D:\Fake\Other.cs", navigated!.FilePath);
        Assert.Null(host.Definitions.Single().Location);
    }

    [Fact]
    public async Task Legacy_requestCompletion_normalizes_to_completion()
    {
        var host = new RecordingCapabilityHost();
        var ctx = CreateContext(host, @"D:\Fake\Legacy.txt", "x");

        await _router.HandleAsync(
            Inbound(CideEditorBusManifest.Legacy.RequestCompletion, requestId: 9, line: 1, column: 1),
            ctx,
            TestContext.Current.CancellationToken);

        Assert.Single(host.Completions);
    }

    private static WorkspaceDiagnosticsCoordinator CreateDiagnostics() =>
        new(new CSharpLanguageService(), new ViewModels.ProblemsPanelViewModel(_ => { }));

    private static CideEditorInboundMessage Inbound(
        string type,
        int? requestId = null,
        int? line = null,
        int? column = null,
        string? lensId = null) =>
        new(type, null, null, null, null, null, requestId, line, column, null, lensId, null, null, null, null, null, null, null);

    private static MonacoEditorCapabilityContext CreateContext(
        RecordingCapabilityHost host,
        string filePath,
        string text,
        ICideEditorLspIntelligence? lsp = null) =>
        new()
        {
            Host = host,
            FilePath = filePath,
            GetEditorText = () => text,
            CSharpLanguage = new CSharpLanguageService(),
            WorkspaceDiagnostics = CreateDiagnostics(),
            ResolveQuickInfoAsync = (_, _, _, _, _) => Task.FromResult<string?>(null),
            CSharpLspHost = lsp,
        };

    private sealed class RecordingCapabilityHost : ICideEditorCapabilityHost
    {
        public List<(int RequestId, IReadOnlyList<CideEditorCompletionItem> Items)> Completions { get; } = [];
        public List<(int RequestId, string? Markdown)> Hovers { get; } = [];
        public List<(int RequestId, string? Signature)> Signatures { get; } = [];
        public List<(int RequestId, CideEditorDefinitionLocation? Location)> Definitions { get; } = [];
        public List<(int RequestId, IReadOnlyList<CideEditorReferenceLocation> Locations)> References { get; } = [];
        public List<(int RequestId, string? Text)> Formats { get; } = [];
        public List<(int RequestId, IReadOnlyList<CideEditorCodeActionItem> Actions)> CodeActions { get; } = [];
        public List<(int RequestId, IReadOnlyList<CideEditorInlayHint> Hints)> InlayHints { get; } = [];
        public List<(int RequestId, IReadOnlyList<CideEditorCodeLensItem> Lenses)> CodeLenses { get; } = [];
        public List<(int RequestId, IReadOnlyList<uint> Data, string? ResultId)> SemanticTokens { get; } = [];

        public Task PushCapabilityCompletionResultAsync(
            int requestId,
            IReadOnlyList<CideEditorCompletionItem> items,
            CancellationToken cancellationToken = default)
        {
            Completions.Add((requestId, items));
            return Task.CompletedTask;
        }

        public Task PushCapabilityHoverResultAsync(
            int requestId,
            string? markdown,
            CancellationToken cancellationToken = default)
        {
            Hovers.Add((requestId, markdown));
            return Task.CompletedTask;
        }

        public Task PushCapabilitySignatureResultAsync(
            int requestId,
            string? signature,
            CancellationToken cancellationToken = default)
        {
            Signatures.Add((requestId, signature));
            return Task.CompletedTask;
        }

        public Task PushCapabilityDefinitionResultAsync(
            int requestId,
            CideEditorDefinitionLocation? location,
            CancellationToken cancellationToken = default)
        {
            Definitions.Add((requestId, location));
            return Task.CompletedTask;
        }

        public Task PushCapabilityReferencesResultAsync(
            int requestId,
            IReadOnlyList<CideEditorReferenceLocation> locations,
            CancellationToken cancellationToken = default)
        {
            References.Add((requestId, locations));
            return Task.CompletedTask;
        }

        public Task PushCapabilityFormatResultAsync(
            int requestId,
            string? text,
            CancellationToken cancellationToken = default)
        {
            Formats.Add((requestId, text));
            return Task.CompletedTask;
        }

        public Task PushCapabilityCodeActionResultAsync(
            int requestId,
            IReadOnlyList<CideEditorCodeActionItem> actions,
            CancellationToken cancellationToken = default)
        {
            CodeActions.Add((requestId, actions));
            return Task.CompletedTask;
        }

        public Task PushCapabilityWorkspaceEditResultAsync(
            int requestId,
            bool ok,
            string? error,
            IReadOnlyList<CideEditorDocumentTextChange> changes,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PushCapabilityInlayHintsResultAsync(
            int requestId,
            IReadOnlyList<CideEditorInlayHint> hints,
            CancellationToken cancellationToken = default)
        {
            InlayHints.Add((requestId, hints));
            return Task.CompletedTask;
        }

        public Task PushCapabilityCodeLensResultAsync(
            int requestId,
            IReadOnlyList<CideEditorCodeLensItem> lenses,
            CancellationToken cancellationToken = default)
        {
            CodeLenses.Add((requestId, lenses));
            return Task.CompletedTask;
        }

        public Task PushCapabilitySemanticTokensResultAsync(
            int requestId,
            IReadOnlyList<uint> data,
            string? resultId,
            CancellationToken cancellationToken = default)
        {
            SemanticTokens.Add((requestId, data, resultId));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLspIntelligence : ICideEditorLspIntelligence
    {
        public bool IsActive => true;
        public bool SupportsSemanticTokens => false;
        public IReadOnlyList<CideEditorCompletionItem> CompletionItems { get; init; } = [];
        public CideEditorDefinitionLocation? Definition { get; init; }
        public int CompletionCalls { get; private set; }

        public Task<IReadOnlyList<CideEditorCompletionItem>> RequestCompletionAsync(
            string filePath,
            string text,
            int line1,
            int col1,
            CancellationToken ct)
        {
            CompletionCalls++;
            return Task.FromResult(CompletionItems);
        }

        public Task<string?> RequestSignatureHelpAsync(string filePath, string text, int line1, int col1, CancellationToken ct) =>
            Task.FromResult<string?>(null);

        public Task<CideEditorDefinitionLocation?> RequestDefinitionAsync(
            string filePath,
            string text,
            int line1,
            int col1,
            CancellationToken ct) =>
            Task.FromResult(Definition);

        public Task<IReadOnlyList<CideEditorReferenceLocation>> RequestReferencesAsync(
            string filePath,
            string text,
            int line1,
            int col1,
            CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CideEditorReferenceLocation>>([]);

        public Task<CideEditorSemanticTokensData?> RequestSemanticTokensFullAsync(
            string filePath,
            string text,
            CancellationToken ct) =>
            Task.FromResult<CideEditorSemanticTokensData?>(null);
    }
}
