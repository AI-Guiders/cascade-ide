#nullable enable

using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SolutionExplorer — flat WPF TreeView of .sln projects (Avalonia keeps full tree SSOT).</summary>
public partial class MainWindow
{
    void RefreshSolutionExplorerTree()
    {
        if (MfdSolutionExplorerTree is null)
            return;

        MfdSolutionExplorerTree.Items.Clear();

        var page = (MfdPages?.SelectedItem as ListBoxItem)?.Content?.ToString();
        if (!string.Equals(page, "SolutionExplorer", StringComparison.OrdinalIgnoreCase))
            return;

        var projects = GlassSolutionExplorerGlance.TryLoadProjectsFromWorkspaceRoot(_session.WorkspaceRoot);
        if (projects is null)
            return;

        foreach (var project in projects)
        {
            var item = new TreeViewItem
            {
                Header = project.Name,
                Tag = GlassSolutionExplorerGlance.TryResolveProjectPath(_session.WorkspaceRoot, project),
            };
            MfdSolutionExplorerTree.Items.Add(item);
        }
    }

    void MfdSolutionExplorerTree_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (MfdSolutionExplorerTree?.SelectedItem is not TreeViewItem { Tag: string path }
            || string.IsNullOrWhiteSpace(path)
            || !File.Exists(path))
            return;

        OpenCodeFile(path);
        e.Handled = true;
    }
}
