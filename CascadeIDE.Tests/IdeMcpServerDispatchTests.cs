using System.Text.Json;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeMcpServerDispatchTests
{
    [Fact]
    public async Task ProxyTool_MapsIdePrefixToCommandId()
    {
        var fake = new FakeActions();
        var args = new Dictionary<string, JsonElement>
        {
            ["path"] = JsonSerializer.SerializeToElement(@"C:\tmp\a.txt")
        };

        var result = await CallToolByConventionAsync(fake, "ide_open_file", args);

        Assert.Equal("OK", result);
        Assert.Equal(IdeCommands.OpenFile, fake.LastCommandId);
        Assert.Same(args, fake.LastArgs);
    }

    [Fact]
    public async Task DispatcherTool_UsesCommandIdFromArguments()
    {
        var fake = new FakeActions();
        var args = new Dictionary<string, JsonElement>
        {
            ["command_id"] = JsonSerializer.SerializeToElement(IdeCommands.ToggleTerminal),
            ["args"] = JsonSerializer.SerializeToElement(new { visible = true })
        };

        var result = await CallToolByConventionAsync(fake, "ide_execute_command", args);

        Assert.Equal("OK", result);
        Assert.Equal(IdeCommands.ToggleTerminal, fake.LastCommandId);
        Assert.NotNull(fake.LastArgs);
        Assert.True(fake.LastArgs!.TryGetValue("visible", out var visEl) && visEl.GetBoolean());
        Assert.False(fake.LastArgs.ContainsKey("args"));
    }

    [Fact]
    public async Task DispatcherTool_MergesNestedArgsObjectForExecuteCommand()
    {
        var fake = new FakeActions();
        var args = new Dictionary<string, JsonElement>
        {
            ["command_id"] = JsonSerializer.SerializeToElement(IdeCommands.DebugLaunch),
            ["args"] = JsonSerializer.SerializeToElement(new
            {
                workspace_path = @"D:\proj",
                target_path = @"C:\Program Files\dotnet\dotnet.exe"
            })
        };

        var result = await CallToolByConventionAsync(fake, "ide_execute_command", args);

        Assert.Equal("OK", result);
        Assert.Equal(IdeCommands.DebugLaunch, fake.LastCommandId);
        Assert.NotNull(fake.LastArgs);
        Assert.Equal(@"D:\proj", fake.LastArgs!["workspace_path"].GetString());
        Assert.Equal(@"C:\Program Files\dotnet\dotnet.exe", fake.LastArgs["target_path"].GetString());
    }

    [Fact]
    public async Task CompatibilityOverride_IdeBuildMapsToBuildStructured()
    {
        var fake = new FakeActions();
        var args = new Dictionary<string, JsonElement>();

        var result = await CallToolByConventionAsync(fake, "ide_build", args);

        Assert.Equal("OK", result);
        Assert.Equal(IdeCommands.BuildStructured, fake.LastCommandId);
    }

    private static async Task<string> CallToolByConventionAsync(
        IIdeMcpActions actions,
        string toolName,
        IReadOnlyDictionary<string, JsonElement>? args)
    {
        var mi = typeof(IdeMcpServer).GetMethod(
            "CallToolByConventionAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(mi);

        var task = (Task<string>)mi!.Invoke(null, new object?[] { actions, toolName, args, CancellationToken.None })!;
        return await task.ConfigureAwait(false);
    }

    private sealed class FakeActions : IIdeMcpActions
    {
        public string? LastCommandId { get; private set; }
        public IReadOnlyDictionary<string, JsonElement>? LastArgs { get; private set; }

        public Task<string> ExecuteCommandAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken = default)
        {
            LastCommandId = commandId;
            LastArgs = args;
            return Task.FromResult("OK");
        }

        public void OpenFile(string path) => throw new NotImplementedException();
        public void LoadSolution(string path) => throw new NotImplementedException();
        public void SelectInEditor(string? filePath, int startLine, int startColumn, int endLine, int endColumn) => throw new NotImplementedException();
        public Task<string> GetEditorStateAsync(int? maxPreviewChars = null) => throw new NotImplementedException();
        public Task<string> GetEditorContentRangeAsync(int startLine, int endLine) => throw new NotImplementedException();
        public Task<string> GetOpenDocumentTextAsync(string? filePath, int? maxChars) => throw new NotImplementedException();
        public void ApplyEdit(string filePath, int startLine, int startColumn, int endLine, int endColumn, string newText) => throw new NotImplementedException();
        public void GoToPosition(string? filePath, int line, int column, int? endLine = null, int? endColumn = null) => throw new NotImplementedException();
        public string GetSolutionInfo() => throw new NotImplementedException();
        public Task<string> GetSolutionFilesAsync() => throw new NotImplementedException();
        public Task<string> SearchWorkspaceTextAsync(string pattern, string? subPath, bool fixedString, string? glob, int maxMatches, string? rgPath) => throw new NotImplementedException();
        public Task<string> GetCurrentFileDiagnosticsAsync() => throw new NotImplementedException();
        public Task<string> GetCodeNavigationContextAsync(string mode, string? filePath, int? line, int? column, int? maxRelated, int? maxNodes, int? maxEdges, string? preset, IReadOnlyList<string>? includeKinds, IReadOnlyList<string>? excludeKinds, string? level) => throw new NotImplementedException();
        public Task<string> BuildAsync() => throw new NotImplementedException();
        public Task<string> BuildStructuredAsync() => throw new NotImplementedException();
        public Task<string> RunTestsAsync() => throw new NotImplementedException();
        public Task<string> RunAffectedTestsAsync(IReadOnlyList<string>? changedPaths = null) => throw new NotImplementedException();
        public Task<string> RunCodeCleanupAsync(string? includePath = null) => throw new NotImplementedException();
        public Task<string> GetCodeMetricsAsync(string? scope = null, string? path = null) => throw new NotImplementedException();
        public Task<string> GetIdeStateAsync() => throw new NotImplementedException();
        public Task<string> GetCockpitSurfaceAsync() => throw new NotImplementedException();
        public Task<string> GetUiModesDiagnosticsAsync() => throw new NotImplementedException();
        public Task<string> GitStatusAsync() => throw new NotImplementedException();
        public Task<string> GitDiffAsync(string? path = null, bool staged = false) => throw new NotImplementedException();
        public Task<string> GitCommitAsync(string message, IReadOnlyList<string>? paths = null) => throw new NotImplementedException();
        public Task<string> GitPushAsync(string? remote = null, string? branch = null, bool dryRun = false) => throw new NotImplementedException();
        public Task<string> GitLogAsync(int n = 20) => throw new NotImplementedException();
        public Task<string> GitFetchAsync(string? remote = null, bool all = false, bool prune = false, bool dryRun = false) => throw new NotImplementedException();
        public Task<string> GitPullAsync(string? remote = null, string? branch = null, bool ffOnly = true, bool dryRun = false) => throw new NotImplementedException();
        public Task<string> GitBranchAsync(string? action = null, string? name = null, string? startPoint = null, bool force = false) => throw new NotImplementedException();
        public Task<string> GitShowAsync(string rev, string? path = null, bool statOnly = false) => throw new NotImplementedException();
        public Task<string> GitSubmoduleAsync(string? action = null, string? path = null, bool recursive = true) => throw new NotImplementedException();
        public Task<string> GitPreflightAsync(bool staged = false, bool includeUntracked = true, bool includePatches = true) => throw new NotImplementedException();
        public Task<string> GitPreflightFixSafeAsync(bool includePatches = true) => throw new NotImplementedException();
        public string GetBuildOutput() => throw new NotImplementedException();
        public void SetBreakpoint(string filePath, int line, string? condition = null) => throw new NotImplementedException();
        public void RemoveBreakpoint(string filePath, int line) => throw new NotImplementedException();
        public void ShowPreview(string title, string content) => throw new NotImplementedException();
        public void ShowEditorPreview() => throw new NotImplementedException();
        public Task<string> RequestConfirmationAsync(string message, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void FocusEditor() => throw new NotImplementedException();
        public string GetUiTheme() => throw new NotImplementedException();
        public Task<string> SetUiThemeAsync(string themeJson) => throw new NotImplementedException();
        public Task<string> GetUiLayoutAsync() => throw new NotImplementedException();
        public Task<string> GetColorsUnderCursorAsync() => throw new NotImplementedException();
        public Task<string> GetControlAppearanceAsync(string? name) => throw new NotImplementedException();
        public Task<string> SetControlLayoutAsync(string controlName, string layoutJson) => throw new NotImplementedException();
        public Task<string> AddControlAsync(string parentName, string controlType, string? content, string? name) => throw new NotImplementedException();
        public Task<string> SetControlTextAsync(string controlName, string text) => throw new NotImplementedException();
        public Task<string> ClickControlAsync(string? controlName) => throw new NotImplementedException();
        public Task<string> SendKeysAsync(string? controlName, string keys) => throw new NotImplementedException();
        public Task<string> SetFocusAsync(string? controlName) => throw new NotImplementedException();
        public Task<string> HighlightControlAsync(string? controlName) => throw new NotImplementedException();
        public Task<string> SetPanelSizeAsync(string panel, double? width, double? height) => throw new NotImplementedException();
        public string GetSupportedEditorLanguages() => throw new NotImplementedException();
        public Task<string> GetDebugSnapshotAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> WriteAgentNotesAsync(string content, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> ReadAgentNotesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> AppendAgentNotesAsync(string content, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> ListAgentNotesRevisionsAsync(int? limit = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> RollbackAgentNotesAsync(string? revisionFile = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> ReadHotContextAsync(string? activeScope = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> RouteContextAsync(string query, string? activeScope = null, int? maxSections = null, int? maxChars = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> MemoryHealthAsync(string? activeScope = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> CompactHotContextAsync(bool apply = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> ExtractFromArchiveAsync(string query, string? revisionFile = null, int? headLimit = null, int? contextLines = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> UpsertAgentNotesSectionAsync(string sectionId, string content, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> SearchAgentNotesAsync(string query, int? headLimit = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> ReadKnowledgeFileAsync(string filePath, int? offset = null, int? limit = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> ListKnowledgeFilesAsync(string? subdir = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> WriteKnowledgeFileAsync(string filePath, string content, string? canonPath = null, bool saveRevision = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> AppendKnowledgeFileAsync(string filePath, string content, string? canonPath = null, bool saveRevision = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> UpsertKnowledgeSectionAsync(string filePath, string sectionId, string content, string? canonPath = null, bool saveRevision = true, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> DeleteKnowledgeFileAsync(string filePath, string? canonPath = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> DeleteKnowledgeSectionAsync(string filePath, string sectionId, string? canonPath = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string> SelectChatMessageAsync(int index) => throw new NotImplementedException();
        public Task<string> GetSelectedChatMessageAsync() => throw new NotImplementedException();
        public Task<string> EditChatAssistantMessageAsync(string messageId, string newContent, string? reason = null) => throw new NotImplementedException();
        public Task<string> ExportChatReadableAsync(bool writeFile = false, string? fileName = null) => throw new NotImplementedException();
    }
}

