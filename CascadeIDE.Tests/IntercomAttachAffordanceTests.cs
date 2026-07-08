using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomAttachAffordanceTests
{
    [Fact]
    public void AttachSelectionToComposer_InsertsMarker_WhenSelectionPresent()
    {
        var vm = CreateChatPanel(
            filePath: @"D:\ws\src\Foo.cs",
            editorText: "line1\nline2\n",
            selectionStart: 0,
            selectionLength: 5);

        var message = vm.AttachSelectionToComposer();

        Assert.Contains("Прикреплено", message, StringComparison.Ordinal);
        Assert.Contains("⟦a:", vm.ChatInput, StringComparison.Ordinal);
    }

    [Fact]
    public void AttachDiagnosticAtCaret_Fails_WhenNoDiagnostic()
    {
        var vm = CreateChatPanel(
            filePath: @"D:\ws\src\Foo.cs",
            editorText: "ok\n",
            caretOffset: 1);

        var message = vm.AttachDiagnosticAtCaretToComposer();

        Assert.Contains("диагност", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyAttachDropPayload_ProblemJson_InsertsMarker()
    {
        var vm = CreateChatPanel(
            filePath: @"D:\ws\src\Foo.cs",
            editorText: "",
            selectionStart: 0,
            selectionLength: 0);

        var payload = IntercomAttachDragFormats.EncodeTextPayload(
            """{"kind":"problem","filePath":"D:\\ws\\src\\Foo.cs","line":3,"severity":"error","id":"CS1001","message":"; expected"}""");

        var message = vm.ApplyAttachDropPayload(payload);

        Assert.Contains("Прикреплено", message, StringComparison.Ordinal);
        Assert.Contains("⟦a:", vm.ChatInput, StringComparison.Ordinal);
    }

    private static ChatPanelViewModel CreateChatPanel(
        string filePath,
        string editorText,
        int selectionStart = 0,
        int selectionLength = 0,
        int caretOffset = 0)
    {
        var minimizer = new ContextMinimizer(new CSharpLanguageService());
        var aiProviderManager = new AiProviderManager(minimizer, _ => (null, ""));
        var vm = new ChatPanelViewModel(
            aiProviderManager,
            () => "ollama",
            () => null,
            () => true,
            () => false,
            () => false,
            () => filePath,
            () => editorText,
            () => @"D:\ws",
            () => "",
            () => "{}",
            () => false,
            () => null,
            getSolutionPath: () => @"D:\ws\app.sln",
            getEditorSelectionStart: () => selectionStart,
            getEditorSelectionLength: () => selectionLength,
            getEditorCaretOffset: () => caretOffset);

        vm.SetDiagnosticStripsAccessor(() => Array.Empty<EditorDiagnosticStrip>());
        return vm;
    }
}
