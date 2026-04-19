using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CascadeIDE.ViewModels;

namespace CascadeIDE;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = ResolveViewType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        TryLogViewResolutionFailure(name, param.GetType());

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        if (data is null)
            return false;

        // Support classic MVVM VMs and Dock's document VMs
        // (e.g., DockDocumentViewModel) that don't inherit ViewModelBase.
        return data is ViewModelBase
               || data.GetType().Name.EndsWith("ViewModel", StringComparison.Ordinal);
    }

    private static Type? ResolveViewType(string fullName)
    {
        var type = Type.GetType(fullName);
        if (type is not null)
            return type;

        type = typeof(ViewLocator).Assembly.GetType(fullName);
        if (type is not null)
            return type;

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(a => a.GetType(fullName, throwOnError: false))
            .FirstOrDefault(t => t is not null);
    }

    private static void TryLogViewResolutionFailure(string viewTypeName, Type viewModelType)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, ".cascade-ide");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "view-locator-log.txt");
            File.AppendAllText(
                path,
                $"[{DateTimeOffset.Now:O}] view='{viewTypeName}' vm='{viewModelType.FullName}'{Environment.NewLine}");
        }
        catch
        {
            // Ignore locator logging failures.
        }
    }
}
