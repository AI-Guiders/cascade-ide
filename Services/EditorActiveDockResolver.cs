#nullable enable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using AvaloniaEdit;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;
using CascadeIDE.Views;

namespace CascadeIDE.Services;

/// <summary>
/// Выбор видимого <see cref="TextEditor"/> при нескольких <see cref="DocumentsDockView"/>
/// (Forward + Mfd + <see cref="MfdHostWindow"/>). ADR 0120/0017.
/// </summary>
public static class EditorActiveDockResolver
{
    public static TextEditor? TryGetEditor(MainWindowViewModel vm, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        TextEditor? best = null;
        var bestScore = int.MinValue;

        foreach (var window in EnumerateHostWindows())
        {
            if (!ReferenceEquals(window.DataContext, vm))
                continue;

            foreach (var dockView in EnumerateDescendants<DockDocumentView>(window))
            {
                if (dockView.DataContext is not DockDocumentViewModel dv)
                    continue;

                if (!string.Equals(dv.Doc.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                var editor = dockView.FindControl<TextEditor>("Editor");
                if (editor is null)
                    continue;

                var score = Score(vm, dockView, window);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = editor;
                }
            }
        }

        return best;
    }

    public static DockDocumentView? TryGetDockDocumentView(MainWindowViewModel vm, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        DockDocumentView? best = null;
        var bestScore = int.MinValue;

        foreach (var window in EnumerateHostWindows())
        {
            if (!ReferenceEquals(window.DataContext, vm))
                continue;

            foreach (var dockView in EnumerateDescendants<DockDocumentView>(window))
            {
                if (dockView.DataContext is not DockDocumentViewModel dv)
                    continue;

                if (!string.Equals(dv.Doc.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                var score = Score(vm, dockView, window);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = dockView;
                }
            }
        }

        return best;
    }

    private static IEnumerable<Window> EnumerateHostWindows()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            yield break;

        foreach (var w in desktop.Windows)
        {
            if (w is Window win)
                yield return win;
        }
    }

    private static int Score(MainWindowViewModel vm, DockDocumentView dockView, Window window)
    {
        var score = 0;
        if (IsVisuallyShown(dockView))
            score += 100;
        if (dockView.IsKeyboardFocusWithin)
            score += 80;
        if (window.IsActive)
            score += 40;

        var inMfdHost = window is MfdHostWindow;
        var inEditorMfdPage = HasAncestor<EditorMfdPageView>(dockView);
        var inForwardDock = HasAncestor<DocumentsDockView>(dockView) && !inMfdHost && !inEditorMfdPage;

        if (vm.PrimaryWorkSurface == PrimaryWorkSurfaceKind.Intercom)
        {
            if (inMfdHost && vm.CurrentMfdShellPage == MfdShellPage.Editor)
                score += 300;
            else if (inEditorMfdPage && vm.IsMfdColumnVisible && vm.CurrentMfdShellPage == MfdShellPage.Editor)
                score += 250;

            if (inForwardDock)
            {
                if (!vm.IsForwardEditorHostVisible)
                    score -= 400;
                else
                    score -= 150;
            }
        }
        else if (inForwardDock && vm.IsForwardEditorHostVisible)
        {
            score += 300;
        }

        if (vm.IsMfdHostWindowShellOpen)
        {
            if (inMfdHost)
                score += 350;
            if (inEditorMfdPage)
                score -= 350;
        }

        return score;
    }

    private static bool IsVisuallyShown(Visual visual)
    {
        for (var cur = visual; cur is not null; cur = cur.GetVisualParent())
        {
            if (cur is Visual v && !v.IsVisible)
                return false;
        }

        return true;
    }

    private static bool HasAncestor<T>(Visual visual) where T : Visual
    {
        for (var cur = visual.GetVisualParent(); cur is not null; cur = cur.GetVisualParent())
        {
            if (cur is T)
                return true;
        }

        return false;
    }

    private static IEnumerable<T> EnumerateDescendants<T>(Visual root) where T : Visual
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is T match)
                yield return match;

            if (child is Visual childVisual)
            {
                foreach (var nested in EnumerateDescendants<T>(childVisual))
                    yield return nested;
            }
        }
    }
}
