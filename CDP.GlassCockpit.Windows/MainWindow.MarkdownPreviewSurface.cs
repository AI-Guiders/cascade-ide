#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using CascadeIDE.Features.Cdp;
using Markdig;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD MarkdownPreview — Markdig AST → FlowDocument rich peel.</summary>
public partial class MainWindow
{
    static readonly MarkdownPipeline MarkdownPipe = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    void RefreshMfdMarkdownVisibility()
    {
        if (MfdMarkdownHost is null || MfdBody is null)
            return;

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        var show = string.Equals(page, "MarkdownPreview", StringComparison.OrdinalIgnoreCase);
        MfdMarkdownHost.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show && MarkdownDocumentViewer is not null && MarkdownDocumentViewer.Document is null)
            RefreshMarkdownPreview();
    }

    bool IsMarkdownHostActive()
    {
        if (MfdMarkdownHost is null)
            return false;
        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        return string.Equals(page, "MarkdownPreview", StringComparison.OrdinalIgnoreCase)
               && MfdMarkdownHost.Visibility == Visibility.Visible;
    }

    internal void MarkdownRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshMarkdownPreview();

    void RefreshMarkdownPreview()
    {
        if (MarkdownDocumentViewer is null)
            return;

        var path = ResolveMarkdownPath();
        if (path is null)
        {
            MarkdownDocumentViewer.Document = new FlowDocument(new Paragraph(new Run("(no .md · open a markdown file or report latch)")));
            if (MarkdownStatusLabel is not null)
                MarkdownStatusLabel.Text = "markdown · empty";
            return;
        }

        try
        {
            var raw = File.ReadAllText(path);
            MarkdownDocumentViewer.Document = GlassMarkdownFlowDocumentBuilder.Build(raw, MarkdownPipe);
            if (MarkdownStatusLabel is not null)
                MarkdownStatusLabel.Text = $"markdown · {Path.GetFileName(path)} · FlowDocument";
        }
        catch (Exception ex)
        {
            MarkdownDocumentViewer.Document = new FlowDocument(new Paragraph(new Run(ex.Message)));
            if (MarkdownStatusLabel is not null)
                MarkdownStatusLabel.Text = "markdown · fail";
        }
    }

    string? ResolveMarkdownPath()
    {
        if (!string.IsNullOrWhiteSpace(_editorPath)
            && _editorPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            && File.Exists(_editorPath))
            return _editorPath;

        // SoftFL: never fall back to pressure-LATEST.md (agent dump wall on Face).
        var report = Path.Combine(CdpHabitatPaths.StateRoot, "report-LATEST.md");
        if (File.Exists(report))
            return report;

        return null;
    }
}
