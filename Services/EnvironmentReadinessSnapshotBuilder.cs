using System.Diagnostics;
using CascadeIDE.Models;
using CascadeIDE.Services.Lsp;

namespace CascadeIDE.Services;

/// <summary>
/// Сборка снимка «готовность окружения» из настроек и уже поднятых LSP-хостов (без дампа environ).
/// </summary>
public static class EnvironmentReadinessSnapshotBuilder
{
    /// <summary>Статическая часть: C# LSP, Markdown LSP (без сетевого вызова).</summary>
    public static IReadOnlyList<EnvironmentReadinessItem> BuildLspRows(
        CascadeIdeSettings settings,
        string? solutionPath,
        CSharpLspDiagnosticsHost? csharpHost,
        MarkdownLspDiagnosticsHost? markdownHost)
    {
        var list = new List<EnvironmentReadinessItem>(4);
        list.Add(BuildCSharpRow(settings, solutionPath, csharpHost));
        list.Add(BuildMarkdownRow(settings, solutionPath, markdownHost));
        return list;
    }

    /// <summary>Проверка <c>dotnet</c> в PATH (как при сборке).</summary>
    public static async Task<EnvironmentReadinessItem> ProbeDotnetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo("dotnet")
            {
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
                return new EnvironmentReadinessItem(
                    "dotnet (SDK / CLI)",
                    "Не удалось запустить процесс dotnet.",
                    EnvironmentReadinessLevel.Unavailable);

            var outTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var ver = (await outTask.ConfigureAwait(false)).Trim();
            var err = (await errTask.ConfigureAwait(false)).Trim();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(ver))
                return new EnvironmentReadinessItem(
                    "dotnet (SDK / CLI)",
                    $"Версия: {ver}",
                    EnvironmentReadinessLevel.Ok);

            var tail = string.IsNullOrWhiteSpace(err) ? $"код выхода {process.ExitCode}" : err;
            return new EnvironmentReadinessItem(
                "dotnet (SDK / CLI)",
                $"dotnet --version не удался ({tail}). Добавь dotnet в PATH или установи SDK.",
                EnvironmentReadinessLevel.Unavailable);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new EnvironmentReadinessItem(
                "dotnet (SDK / CLI)",
                $"Не удалось выполнить dotnet --version: {ex.Message}",
                EnvironmentReadinessLevel.Unavailable);
        }
    }

    private static EnvironmentReadinessItem BuildCSharpRow(
        CascadeIdeSettings settings,
        string? solutionPath,
        CSharpLspDiagnosticsHost? host)
    {
        var provider = string.IsNullOrWhiteSpace(settings.CSharpLsp.Provider)
            ? CSharpLspProviderIds.ParseOnly
            : settings.CSharpLsp.Provider.Trim();

        if (string.Equals(provider, CSharpLspProviderIds.ParseOnly, StringComparison.OrdinalIgnoreCase))
        {
            return new EnvironmentReadinessItem(
                "C# LSP",
                "Режим «только парсер»: отдельный процесс language server не используется (Roslyn в процессе IDE).",
                EnvironmentReadinessLevel.Info);
        }

        var slnOk = !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
        if (!slnOk)
        {
            return new EnvironmentReadinessItem(
                "C# LSP",
                $"Провайдер: {provider}. Открой файл решения (.sln/.slnx), чтобы IDE могла запустить LSP.",
                EnvironmentReadinessLevel.Warning);
        }

        var (exe, _) = CSharpLspProviderIds.ResolveProcessArgs(
            provider,
            solutionPath,
            settings.CSharpLsp.Executable,
            settings.CSharpLsp.Arguments);

        var exeHint = string.IsNullOrWhiteSpace(exe) ? provider : $"{provider}: {exe}";

        if (host is null)
        {
            return new EnvironmentReadinessItem(
                "C# LSP",
                $"{exeHint}. Процесс не запущен — проверь путь к исполняемому файлу и аргументы в настройках.",
                EnvironmentReadinessLevel.Warning);
        }

        if (host.IsActive)
        {
            return new EnvironmentReadinessItem(
                "C# LSP",
                $"{exeHint}. Процесс активен.",
                EnvironmentReadinessLevel.Ok);
        }

        return new EnvironmentReadinessItem(
            "C# LSP",
            $"{exeHint}. Процесс не активен (завершился или не прошёл handshake).",
            EnvironmentReadinessLevel.Warning);
    }

    private static EnvironmentReadinessItem BuildMarkdownRow(
        CascadeIdeSettings settings,
        string? solutionPath,
        MarkdownLspDiagnosticsHost? host)
    {
        var provider = string.IsNullOrWhiteSpace(settings.MarkdownLsp.Provider)
            ? MarkdownLspProviderIds.Off
            : settings.MarkdownLsp.Provider.Trim();

        if (string.Equals(provider, MarkdownLspProviderIds.Off, StringComparison.OrdinalIgnoreCase))
        {
            return new EnvironmentReadinessItem(
                "Markdown LSP",
                "Выключено (диагностики Markdown из отдельного сервера не используются).",
                EnvironmentReadinessLevel.Info);
        }

        var slnOk = !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
        if (!slnOk)
        {
            return new EnvironmentReadinessItem(
                "Markdown LSP",
                $"Провайдер: {provider}. Нужен открытый файл решения, чтобы запустить сервер.",
                EnvironmentReadinessLevel.Warning);
        }

        var (exe, args) = MarkdownLspProviderIds.ResolveProcessArgs(
            provider,
            settings.MarkdownLsp.Executable,
            settings.MarkdownLsp.Arguments);

        var argHint = string.IsNullOrWhiteSpace(args) ? "" : $" {args}";
        var exeHint = string.IsNullOrWhiteSpace(exe) ? $"{provider}" : $"{provider}: {exe}{argHint}";

        if (host is null)
        {
            return new EnvironmentReadinessItem(
                "Markdown LSP",
                $"{exeHint}. Процесс не запущен — проверь установку (например Marksman в PATH) или путь в настройках.",
                EnvironmentReadinessLevel.Warning);
        }

        if (host.IsActive)
        {
            return new EnvironmentReadinessItem(
                "Markdown LSP",
                $"{exeHint}. Процесс активен.",
                EnvironmentReadinessLevel.Ok);
        }

        return new EnvironmentReadinessItem(
            "Markdown LSP",
            $"{exeHint}. Процесс не активен.",
            EnvironmentReadinessLevel.Warning);
    }

    /// <summary>
    /// Полный набор строк для страницы «готовность окружения» (ADR 0023): LSP, затем проверка <c>dotnet</c>.
    /// Дополнительные проверки (MCP, переменные окружения и т.д.) добавлять сюда, чтобы не раздувать ViewModel.
    /// </summary>
    public static async Task<IReadOnlyList<EnvironmentReadinessItem>> BuildAllRowsAsync(
        CascadeIdeSettings settings,
        string? solutionPath,
        CSharpLspDiagnosticsHost? csharpHost,
        MarkdownLspDiagnosticsHost? markdownHost,
        CancellationToken cancellationToken = default)
    {
        var lsp = BuildLspRows(settings, solutionPath, csharpHost, markdownHost);
        var dotnet = await ProbeDotnetAsync(cancellationToken).ConfigureAwait(false);
        var combined = new List<EnvironmentReadinessItem>(lsp.Count + 1);
        combined.AddRange(lsp);
        combined.Add(dotnet);
        return combined;
    }
}
