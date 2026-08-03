#nullable enable

using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using CascadeIDE.SoftOrgan;

namespace CDP.GlassCockpit.Windows;

/// <summary>Glass MFD SolutionExplorer — nested TreeView: projects + *.cs children (cap 200).</summary>
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
            var projectPath = GlassSolutionExplorerGlance.TryResolveProjectPath(_session.WorkspaceRoot, project);
            var item = new TreeViewItem
            {
                Header = project.Name,
                Tag = projectPath,
                IsExpanded = false,
            };

            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                foreach (var csPath in GlassSolutionExplorerGlance.EnumerateProjectCsFiles(projectPath))
                {
                    item.Items.Add(new TreeViewItem
                    {
                        Header = Path.GetFileName(csPath),
                        Tag = csPath,
                    });
                }
            }

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
