#nullable enable
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>
/// <c>dotnet new</c> + <c>dotnet sln add</c> в открытое решение (ADR 0107 §6, ADR 0125 W2).
/// </summary>
public static class ProjectInSolutionCreator
{
    public static readonly IReadOnlySet<string> WhitelistedTemplates =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "console", "classlib", "webapi" };

    public static async Task<ProjectInSolutionCreateResult> TryCreateAsync(
        string? solutionFilePath,
        string template,
        string projectName,
        IDotnetCommandRunner dotnetRunner,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dotnetRunner);

        if (string.IsNullOrWhiteSpace(solutionFilePath))
            return ProjectInSolutionCreateResult.Failure("Сначала открой решение (.sln).");

        if (!WhitelistedTemplates.Contains(template.Trim()))
        {
            return ProjectInSolutionCreateResult.Failure(
                "Шаблон не поддерживается. Доступны: console, classlib, webapi.");
        }

        var name = projectName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return ProjectInSolutionCreateResult.Failure("Укажи имя проекта в хвосте: /solution new console MyApp");

        string slnPath;
        try
        {
            slnPath = CanonicalFilePath.Normalize(solutionFilePath.Trim());
        }
        catch (Exception ex)
        {
            return ProjectInSolutionCreateResult.Failure("Некорректный путь к решению: " + ex.Message);
        }

        if (!slnPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            return ProjectInSolutionCreateResult.Failure("Нужен открытый файл .sln.");

        if (!File.Exists(slnPath))
            return ProjectInSolutionCreateResult.Failure("Файл решения не найден: " + slnPath);

        var solutionDir = Path.GetDirectoryName(slnPath);
        if (string.IsNullOrWhiteSpace(solutionDir))
            return ProjectInSolutionCreateResult.Failure("Не удалось определить каталог решения.");

        var outputDir = Path.Combine(solutionDir, name);
        if (Directory.Exists(outputDir) || File.Exists(outputDir))
            return ProjectInSolutionCreateResult.Failure("Каталог или файл уже существует: " + outputDir);

        var templateId = template.Trim().ToLowerInvariant();
        IReadOnlyList<string> newArgs = ["new", templateId, "-n", name, "-o", outputDir];
        var (newOk, newExit, newOutput) = await dotnetRunner
            .RunAsync(newArgs, workingDirectory: solutionDir, cancellationToken)
            .ConfigureAwait(false);

        if (!newOk)
        {
            var tail = string.IsNullOrWhiteSpace(newOutput) ? "" : "\r\n" + newOutput.Trim();
            return ProjectInSolutionCreateResult.Failure($"dotnet new {templateId} завершился с кодом {newExit}.{tail}");
        }

        var csproj = Directory.EnumerateFiles(outputDir, "*.csproj", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(csproj))
        {
            return ProjectInSolutionCreateResult.Failure(
                "Проект создан, но .csproj не найден в " + outputDir);
        }

        IReadOnlyList<string> slnAddArgs = ["sln", "add", csproj];
        var (addOk, addExit, addOutput) = await dotnetRunner
            .RunAsync(slnAddArgs, workingDirectory: solutionDir, cancellationToken)
            .ConfigureAwait(false);

        if (!addOk)
        {
            var tail = string.IsNullOrWhiteSpace(addOutput) ? "" : "\r\n" + addOutput.Trim();
            return ProjectInSolutionCreateResult.Failure($"dotnet sln add завершился с кодом {addExit}.{tail}");
        }

        return ProjectInSolutionCreateResult.Success(csproj);
    }
}

public readonly record struct ProjectInSolutionCreateResult(bool Ok, string? ProjectPath, string? ErrorMessage)
{
    public static ProjectInSolutionCreateResult Success(string projectPath) => new(true, projectPath, null);

    public static ProjectInSolutionCreateResult Failure(string message) => new(false, null, message);
}
