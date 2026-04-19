using System.Diagnostics;
using CascadeIDE.Models;
using CascadeIDE.Services.Lsp;

namespace CascadeIDE.Services;

/// <summary>
/// Сборка снимка «готовность окружения» из настроек и уже поднятых LSP-хостов (без дампа environ).
/// </summary>
public static class EnvironmentReadinessSnapshotBuilder
{
    /// <summary>Заглушка для ячейки «агент»: отдельная проверка MCP/ACP — по мере появления контракта.</summary>
    private static AnnunciatorLampItem BuildAgentRow() =>
        new(
            EnvironmentReadinessCellIds.Agent,
            "Агент (AI)",
            "Канал к агенту и MCP задаётся сессией IDE; отдельный health-check на этой странице пока не выполняется.",
            AnnunciatorLampLevel.Advisory,
            LampShortLabel: "AI");

    /// <summary>Статическая часть: C# LSP, Markdown LSP (без сетевого вызова).</summary>
    public static IReadOnlyList<AnnunciatorLampItem> BuildLspRows(
        CascadeIdeSettings settings,
        string? solutionPath,
        CSharpLspDiagnosticsHost? csharpHost,
        MarkdownLspDiagnosticsHost? markdownHost)
    {
        var list = new List<AnnunciatorLampItem>(4);
        list.Add(BuildCSharpRow(settings, solutionPath, csharpHost));
        list.Add(BuildMarkdownRow(settings, solutionPath, markdownHost));
        return list;
    }

    /// <summary>Проверка <c>dotnet</c> в PATH (как при сборке).</summary>
    public static async Task<AnnunciatorLampItem> ProbeDotnetAsync(CancellationToken cancellationToken = default)
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
                return new AnnunciatorLampItem(
                    EnvironmentReadinessCellIds.DotnetSdk,
                    "dotnet (SDK / CLI)",
                    "Не удалось запустить процесс dotnet.",
                    AnnunciatorLampLevel.Critical,
                    LampShortLabel: ".NET");

            var outTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var ver = (await outTask.ConfigureAwait(false)).Trim();
            var err = (await errTask.ConfigureAwait(false)).Trim();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(ver))
                return new AnnunciatorLampItem(
                    EnvironmentReadinessCellIds.DotnetSdk,
                    "dotnet (SDK / CLI)",
                    $"Версия: {ver}",
                    AnnunciatorLampLevel.Ok,
                    LampShortLabel: ".NET");

            var tail = string.IsNullOrWhiteSpace(err) ? $"код выхода {process.ExitCode}" : err;
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.DotnetSdk,
                "dotnet (SDK / CLI)",
                $"dotnet --version не удался ({tail}). Добавь dotnet в PATH или установи SDK.",
                AnnunciatorLampLevel.Critical,
                LampShortLabel: ".NET");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.DotnetSdk,
                "dotnet (SDK / CLI)",
                $"Не удалось выполнить dotnet --version: {ex.Message}",
                AnnunciatorLampLevel.Critical,
                LampShortLabel: ".NET");
        }
    }

    private static AnnunciatorLampItem BuildCSharpRow(
        CascadeIdeSettings settings,
        string? solutionPath,
        CSharpLspDiagnosticsHost? host)
    {
        var provider = string.IsNullOrWhiteSpace(settings.Languages.CSharp.Provider)
            ? CSharpLspProviderIds.ParseOnly
            : settings.Languages.CSharp.Provider.Trim();

        if (string.Equals(provider, CSharpLspProviderIds.ParseOnly, StringComparison.OrdinalIgnoreCase))
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.CSharpLsp,
                "C# LSP",
                "Режим «только парсер»: отдельный процесс language server не используется (Roslyn в процессе IDE).",
                AnnunciatorLampLevel.Advisory,
                LampShortLabel: "C#");
        }

        var slnOk = !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
        if (!slnOk)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.CSharpLsp,
                "C# LSP",
                $"Провайдер: {provider}. Открой файл решения (.sln/.slnx), чтобы IDE могла запустить LSP.",
                AnnunciatorLampLevel.Caution,
                LampShortLabel: "C#");
        }

        var (exe, _) = CSharpLspProviderIds.ResolveProcessArgs(
            provider,
            solutionPath,
            settings.Languages.CSharp.Executable,
            settings.Languages.CSharp.Arguments);

        var exeHint = string.IsNullOrWhiteSpace(exe) ? provider : $"{provider}: {exe}";

        if (host is null)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.CSharpLsp,
                "C# LSP",
                $"{exeHint}. Процесс не запущен — проверь путь к исполняемому файлу и аргументы в настройках.",
                AnnunciatorLampLevel.Caution,
                LampShortLabel: "C#");
        }

        if (host.IsActive)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.CSharpLsp,
                "C# LSP",
                $"{exeHint}. Процесс активен.",
                AnnunciatorLampLevel.Ok,
                LampShortLabel: "C#");
        }

        return new AnnunciatorLampItem(
            EnvironmentReadinessCellIds.CSharpLsp,
            "C# LSP",
            $"{exeHint}. Процесс не активен (завершился или не прошёл handshake).",
            AnnunciatorLampLevel.Caution,
            LampShortLabel: "C#");
    }

    private static AnnunciatorLampItem BuildMarkdownRow(
        CascadeIdeSettings settings,
        string? solutionPath,
        MarkdownLspDiagnosticsHost? host)
    {
        var provider = string.IsNullOrWhiteSpace(settings.Languages.Markdown.Provider)
            ? MarkdownLspProviderIds.Off
            : settings.Languages.Markdown.Provider.Trim();

        if (string.Equals(provider, MarkdownLspProviderIds.Off, StringComparison.OrdinalIgnoreCase))
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.MarkdownLsp,
                "Markdown LSP",
                "Выключено (диагностики Markdown из отдельного сервера не используются).",
                AnnunciatorLampLevel.Advisory,
                LampShortLabel: "MD");
        }

        var slnOk = !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath);
        if (!slnOk)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.MarkdownLsp,
                "Markdown LSP",
                $"Провайдер: {provider}. Нужен открытый файл решения, чтобы запустить сервер.",
                AnnunciatorLampLevel.Caution,
                LampShortLabel: "MD");
        }

        var (exe, args) = MarkdownLspProviderIds.ResolveProcessArgs(
            provider,
            settings.Languages.Markdown.Executable,
            settings.Languages.Markdown.Arguments);

        var argHint = string.IsNullOrWhiteSpace(args) ? "" : $" {args}";
        var exeHint = string.IsNullOrWhiteSpace(exe) ? $"{provider}" : $"{provider}: {exe}{argHint}";

        if (host is null)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.MarkdownLsp,
                "Markdown LSP",
                $"{exeHint}. Процесс не запущен — проверь установку (например Marksman в PATH) или путь в настройках.",
                AnnunciatorLampLevel.Caution,
                LampShortLabel: "MD");
        }

        if (host.IsActive)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.MarkdownLsp,
                "Markdown LSP",
                $"{exeHint}. Процесс активен.",
                AnnunciatorLampLevel.Ok,
                LampShortLabel: "MD");
        }

        return new AnnunciatorLampItem(
            EnvironmentReadinessCellIds.MarkdownLsp,
            "Markdown LSP",
            $"{exeHint}. Процесс не активен.",
            AnnunciatorLampLevel.Caution,
            LampShortLabel: "MD");
    }

    /// <summary>
    /// Полный набор строк для страницы «готовность окружения» (ADR 0023): LSP, затем проверка <c>dotnet</c>.
    /// Дополнительные проверки (MCP, переменные окружения и т.д.) добавлять сюда, чтобы не раздувать ViewModel.
    /// </summary>
    public static async Task<IReadOnlyList<AnnunciatorLampItem>> BuildAllRowsAsync(
        CascadeIdeSettings settings,
        string? solutionPath,
        CSharpLspDiagnosticsHost? csharpHost,
        MarkdownLspDiagnosticsHost? markdownHost,
        CancellationToken cancellationToken = default)
    {
        var agent = BuildAgentRow();
        var lsp = BuildLspRows(settings, solutionPath, csharpHost, markdownHost);
        var dotnet = await ProbeDotnetAsync(cancellationToken).ConfigureAwait(false);
        var combined = new List<AnnunciatorLampItem>(lsp.Count + 2);
        combined.Add(agent);
        combined.AddRange(lsp);
        combined.Add(dotnet);
        return combined;
    }
}
