using Xunit;

namespace CascadeIDE.Tests.VisualVerification;

/// <summary>
/// Сравнение PNG с эталоном в <c>TestData/Visual/*.approved.png</c>.
/// Обновление эталона: установить переменную окружения <c>CASCADE_IDE_UPDATE_VISUAL_BASELINES=1</c> и прогнать тест (запишет файл в исходники проекта тестов).
/// </summary>
public static class ApprovedPngAssert
{
    public const string UpdateBaselinesEnvironmentVariable = "CASCADE_IDE_UPDATE_VISUAL_BASELINES";

    public static void EqualToApproved(byte[] actualPng, string approvedRelativeFileName)
    {
        ArgumentNullException.ThrowIfNull(actualPng);
        ArgumentException.ThrowIfNullOrEmpty(approvedRelativeFileName);

        var outputPath = Path.Combine(VisualBaselinePaths.OutputVisualDirectory, approvedRelativeFileName);
        var updateRequested = string.Equals(
            Environment.GetEnvironmentVariable(UpdateBaselinesEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

        if (updateRequested)
        {
            var sourceDir = VisualBaselinePaths.SourceVisualDirectory;
            Directory.CreateDirectory(sourceDir);
            var sourcePath = Path.Combine(sourceDir, approvedRelativeFileName);
            File.WriteAllBytes(sourcePath, actualPng);

            Directory.CreateDirectory(VisualBaselinePaths.OutputVisualDirectory);
            File.WriteAllBytes(outputPath, actualPng);
            return;
        }

        Assert.True(File.Exists(outputPath), $"Отсутствует эталон (скопируйте из репозитория): {outputPath}");

        var expected = File.ReadAllBytes(outputPath);
        if (expected.AsSpan().SequenceEqual(actualPng))
            return;

        Assert.Fail(
            $"PNG не совпал с эталоном: {approvedRelativeFileName}. " +
            $"Чтобы перезаписать эталон осознанно, задай {UpdateBaselinesEnvironmentVariable}=1 и перезапусти тест.");
    }
}
