#nullable enable

namespace CascadeIDE.ArchitectureAnalyzers;

internal static class ArchitectureLayerPaths
{
    public static bool IsFeaturesApplicationPath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        var n = Normalize(filePath);
        return n.Contains("/Features/", StringComparison.OrdinalIgnoreCase)
            && n.Contains("/Application/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsComputingUnitFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        return Normalize(filePath).Contains("/Cockpit/ComputingUnits/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsViewModelsPath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        return Normalize(filePath).Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMainWindowPresentationPartial(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        var n = Normalize(filePath);
        return n.EndsWith("/MainWindowViewModel.Presentation.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string filePath) => filePath.Replace('\\', '/');
}
