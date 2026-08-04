#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace CDP.GlassCockpit.Windows;

/// <summary>Forward peeled UserControl fields so existing MainWindow surfaces keep short names.</summary>
public partial class MainWindow
{
    Border? PaletteOverlay => Overlays?.PaletteOverlay;
    TextBox? PaletteQuery => Overlays?.PaletteQuery;
    ListBox? PaletteList => Overlays?.PaletteList;
    Border? ChordOverlay => Overlays?.ChordOverlay;
    TextBox? ChordQuery => Overlays?.ChordQuery;
    ListBox? ChordList => Overlays?.ChordList;

    Border? MfdTerminalHost => MfdHosts?.MfdTerminalHost;
    TextBlock? TerminalShellLabel => MfdHosts?.TerminalShellLabel;
    EasyWindowsTerminalControl.EasyTerminalControl? TerminalVt => MfdHosts?.TerminalVt;

    Border? MfdBuildHost => MfdHosts?.MfdBuildHost;
    TextBlock? BuildStatusLabel => MfdHosts?.BuildStatusLabel;
    ListBox? BuildProblemsList => MfdHosts?.BuildProblemsList;
    TextBox? BuildOutput => MfdHosts?.BuildOutput;

    Border? MfdTestsHost => MfdHosts?.MfdTestsHost;
    TextBlock? TestsStatusLabel => MfdHosts?.TestsStatusLabel;
    ListBox? TestsFailList => MfdHosts?.TestsFailList;
    TextBox? TestsOutput => MfdHosts?.TestsOutput;

    Border? MfdGitHost => MfdHosts?.MfdGitHost;
    TextBlock? GitStatusLabel => MfdHosts?.GitStatusLabel;
    ListBox? GitList => MfdHosts?.GitList;
    FlowDocumentScrollViewer? GitDiffViewer => MfdHosts?.GitDiffViewer;
    TextBox? GitCommitMessage => MfdHosts?.GitCommitMessage;

    Border? MfdProblemsHost => MfdHosts?.MfdProblemsHost;
    TextBlock? ProblemsStatusLabel => MfdHosts?.ProblemsStatusLabel;
    ListBox? ProblemsList => MfdHosts?.ProblemsList;
    Border? ProblemsErrCard => MfdHosts?.ProblemsErrCard;
    Border? ProblemsWarnCard => MfdHosts?.ProblemsWarnCard;
    Border? ProblemsAllCard => MfdHosts?.ProblemsAllCard;
    TextBlock? ProblemsErrCount => MfdHosts?.ProblemsErrCount;
    TextBlock? ProblemsWarnCount => MfdHosts?.ProblemsWarnCount;
    TextBlock? ProblemsAllCount => MfdHosts?.ProblemsAllCount;

    Border? MfdRelatedFilesHost => MfdHosts?.MfdRelatedFilesHost;
    TextBlock? RelatedStatusLabel => MfdHosts?.RelatedStatusLabel;
    ListBox? RelatedList => MfdHosts?.RelatedList;
    GlassSemanticMapSkia? RelatedSkia => MfdHosts?.RelatedSkia;

    Border? MfdHybridIndexHost => MfdHosts?.MfdHybridIndexHost;
    TextBlock? HybridIndexStatusLabel => MfdHosts?.HybridIndexStatusLabel;
    ItemsControl? HybridIndexCardsPanel => MfdHosts?.HybridIndexCardsPanel;
    GlassSemanticMapSkia? HybridSkia => MfdHosts?.HybridSkia;
    ListBox? HybridScopeList => MfdHosts?.HybridScopeList;

    Border? MfdGlanceCardsHost => MfdHosts?.MfdGlanceCardsHost;
    TextBlock? GlanceCardsStatusLabel => MfdHosts?.GlanceCardsStatusLabel;
    ItemsControl? GlanceCardsPanel => MfdHosts?.GlanceCardsPanel;

    Border? MfdSemanticMapHost => MfdHosts?.MfdSemanticMapHost;
    TextBlock? SemanticStatusLabel => MfdHosts?.SemanticStatusLabel;
    ItemsControl? SemanticArchCardsPanel => MfdHosts?.SemanticArchCardsPanel;
    ListBox? SemanticArchRoleList => MfdHosts?.SemanticArchRoleList;
    ListBox? SemanticList => MfdHosts?.SemanticList;
    GlassSemanticMapSkia? SemanticSkia => MfdHosts?.SemanticSkia;

    Border? MfdCorrespondenceHost => MfdHosts?.MfdCorrespondenceHost;
    TextBlock? CorrespondenceStatusLabel => MfdHosts?.CorrespondenceStatusLabel;
    ItemsControl? CorrespondenceCardsPanel => MfdHosts?.CorrespondenceCardsPanel;
    ListBox? CorrespondenceTimelineList => MfdHosts?.CorrespondenceTimelineList;

    Border? MfdMarkdownHost => MfdHosts?.MfdMarkdownHost;
    TextBlock? MarkdownStatusLabel => MfdHosts?.MarkdownStatusLabel;
    FlowDocumentScrollViewer? MarkdownDocumentViewer => MfdHosts?.MarkdownDocumentViewer;

    Border? MfdAiChatSettingsHost => MfdHosts?.MfdAiChatSettingsHost;
    TextBlock? AiChatSettingsStatusLabel => MfdHosts?.AiChatSettingsStatusLabel;
    TextBox? AiChatSettingsToml => MfdHosts?.AiChatSettingsToml;

    Border? MfdDebugStackHost => MfdHosts?.MfdDebugStackHost;
    TextBlock? DebugStatusLabel => MfdHosts?.DebugStatusLabel;
    ListBox? DebugStackList => MfdHosts?.DebugStackList;
    ListBox? DebugLocalsList => MfdHosts?.DebugLocalsList;

    Border? MfdWebAiHost => MfdHosts?.MfdWebAiHost;
    TextBox? WebAiUrl => MfdHosts?.WebAiUrl;
    TextBlock? WebAiStatusLabel => MfdHosts?.WebAiStatusLabel;
    Microsoft.Web.WebView2.Wpf.WebView2? WebAiView => MfdHosts?.WebAiView;
}
