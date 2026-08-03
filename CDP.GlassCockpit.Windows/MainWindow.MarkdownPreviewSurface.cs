#nullable enable

using System.IO;
using System.Windows;
using System.Windows.Controls;
using CascadeIDE.Features.Cdp;
using Markdig;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD MarkdownPreview — Markdig plain text (Avalonia Markdig control peel v1).</summary>
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

        if (show && MarkdownOutput is not null && string.IsNullOrEmpty(MarkdownOutput.Text))
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
        if (MarkdownOutput is null)
            return;

        var path = ResolveMarkdownPath();
        if (path is null)
        {
            MarkdownOutput.Text = "(no .md · open a markdown file or report latch)";
            if (MarkdownStatusLabel is not null)
                MarkdownStatusLabel.Text = "markdown · empty";
            return;
        }

        try
        {
            var raw = File.ReadAllText(path);
            MarkdownOutput.Text = Markdown.ToPlainText(raw, MarkdownPipe);
            if (MarkdownStatusLabel is not null)
                MarkdownStatusLabel.Text = $"markdown · {Path.GetFileName(path)} · Markdig plain";
        }
        catch (Exception ex)
        {
            MarkdownOutput.Text = ex.Message;
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

        var report = Path.Combine(CdpHabitatPaths.StateRoot, "report-LATEST.md");
        if (File.Exists(report))
            return report;

        var pressure = Path.Combine(CdpHabitatPaths.StateRoot, "cdp", "pressure-LATEST.md");
        if (File.Exists(pressure))
            return pressure;

        return null;
    }
}
