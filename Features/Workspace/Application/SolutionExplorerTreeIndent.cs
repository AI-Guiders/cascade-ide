using Avalonia.Controls;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>Фиксированный горизонтальный отступ узлов SE (ADR 0167 §2.5).</summary>
public static class SolutionExplorerTreeIndent
{
    public const double StepPixels = 17;

    public static int GetDepth(TreeViewItem item)
    {
        var depth = 0;
        var parent = item.Parent;
        while (parent is not null)
        {
            if (parent is TreeViewItem)
                depth++;
            parent = parent.Parent;
        }

        return depth;
    }

    public static void Apply(TreeViewItem item)
    {
        var depth = GetDepth(item);
        item.Padding = new Avalonia.Thickness(2, 2);
        item.Margin = new Avalonia.Thickness(depth * StepPixels, 0, 0, 0);
    }
}
