namespace CascadeIDE.Tests.VisualVerification;

/// <summary>Пути к каталогу <c>CascadeIDE.Tests/TestData/Visual</c> (и копии в output).</summary>
internal static class VisualBaselinePaths
{
    /// <summary>Каталог с approved-файлами рядом с тестовой сборкой (CopyToOutputDirectory).</summary>
    public static string OutputVisualDirectory =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "Visual");

    /// <summary>
    /// Исходный каталог в репозитории (на три уровня выше <c>net10.0</c> — корень проекта тестов).
    /// Используется только при <c>CASCADE_IDE_UPDATE_VISUAL_BASELINES=1</c>.
    /// </summary>
    public static string SourceVisualDirectory
    {
        get
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            // .../CascadeIDE.Tests/bin/Debug/net10.0  →  .../CascadeIDE.Tests
            var testsProject = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            return Path.Combine(testsProject, "TestData", "Visual");
        }
    }
}
