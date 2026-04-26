using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;
using CascadeIDE.Services.Lsp;

namespace CascadeIDE.Features.EnvironmentReadiness.Application;

/// <summary>
/// Сборка снимка «готовность окружения» из настроек и проекции <see cref="IdeHostStateChanged"/> (тот же снимок, что на DataBus; без дампа environ).
/// </summary>
public static class EnvironmentReadinessSnapshotBuilder
{
    /// <summary>Сводная строка блока Dev Tools: агент, LSP, dotnet — лампа «DEV» гаснет, если по всем деталям только Ok/Advisory.</summary>
    public static AnnunciatorLampItem BuildDevToolsSectionRow(IReadOnlyList<AnnunciatorLampItem> devToolsDetailRows)
    {
        if (devToolsDetailRows.Count == 0)
            throw new ArgumentException("Expected at least one detail row.", nameof(devToolsDetailRows));

        var worst = devToolsDetailRows[0].Level;
        for (var i = 1; i < devToolsDetailRows.Count; i++)
            worst = WorstAnnunciatorLevel(worst, devToolsDetailRows[i].Level);

        var level = AggregateSectionLampLevelFromWorstChild(worst);
        var detail = level == AnnunciatorLampLevel.Ok
            ? ""
            : "Есть замечания уровня Caution или выше — см. строки ниже.";
        return new AnnunciatorLampItem(
            EnvironmentReadinessCellIds.DevToolsSection,
            "Dev Tools",
            detail,
            level,
            LampShortLabel: "DEV");
    }

    /// <summary>Ячейка «агент»: режим моста MCP (stdio) / ACP / без внешнего моста (Off = <see cref="AnnunciatorLampLevel.Caution"/>).</summary>
    private static AnnunciatorLampItem BuildAgentRow(bool isMcpStdioHost, string? activeAiProvider)
    {
        if (isMcpStdioHost)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.Agent,
                "Агент (MCP)",
                "Запуск с --mcp-stdio: внешний хост вызывает инструменты этой сессии CascadeIDE (см. MCP-PROTOCOL.md).",
                AnnunciatorLampLevel.Advisory,
                LampShortLabel: "MCP");
        }

        if (string.Equals(activeAiProvider, "CursorACP", StringComparison.Ordinal))
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.Agent,
                "Агент (ACP)",
                "Чат через Cursor ACP: сессия cursor-agent и mcpServers из настроек ([mcp], ADR 0048).",
                AnnunciatorLampLevel.Advisory,
                LampShortLabel: "ACP");
        }

        return new AnnunciatorLampItem(
            EnvironmentReadinessCellIds.Agent,
            "Агент (нет моста)",
            "Нет ни --mcp-stdio, ни провайдера Cursor ACP: внешний контур агента к этой IDE не подключён (встроенные провайдеры — без моста к хосту).",
            AnnunciatorLampLevel.Caution,
            LampShortLabel: "Off");
    }

    /// <summary>Переменные окружения и пути, которые IDE читает для agent-notes / knowledge / netcoredbg (значения в UI не показываем).</summary>
    /// <param name="tryResolveNetcoreDbgWhenUnset">
    /// Опционально: подмена результата поиска <c>netcoredbg</c>, когда <c>NETCOREDBG_PATH</c> пуста (только для тестов).
    /// Если параметр не передан — вызывается <see cref="EnvironmentReadinessExecutablePathProbe.TryResolveExecutablePath"/> для имени <c>netcoredbg</c>.
    /// Если делегат передан — используется только его возврат (в т.ч. <see langword="null"/> = «не найден»), без обращения к реальному PATH.
    /// </param>
    public static IReadOnlyList<AnnunciatorLampItem> BuildEnvProbeRows(
        EnvironmentReadinessEnvSnapshot env,
        Func<string?>? tryResolveNetcoreDbgWhenUnset = null) =>
    [
        BuildAgentNotesFileRow(env.AgentNotesFile),
        BuildAgentNotesCanonRow(env.AgentNotesCanonPath),
        BuildNetcoreDbgRow(env.NetcoreDbgPath, tryResolveNetcoreDbgWhenUnset),
    ];

    /// <summary>
    /// Сводная строка блока env: лампа «ENV» гаснет (<see cref="AnnunciatorLampLevel.Ok"/>), если по трём проверкам нет Caution/Critical
    /// (только Ok и Advisory — опционально не задано).
    /// </summary>
    public static AnnunciatorLampItem BuildEnvSectionRow(IReadOnlyList<AnnunciatorLampItem> envProbeRows)
    {
        if (envProbeRows.Count != 3)
            throw new ArgumentOutOfRangeException(nameof(envProbeRows), envProbeRows.Count, "Expected Notes, KB, Dbg.");

        var level = AggregateEnvBlockLevel(envProbeRows[0].Level, envProbeRows[1].Level, envProbeRows[2].Level);
        var detail = level == AnnunciatorLampLevel.Ok
            ? ""
            : "Есть замечания уровня Caution или выше — см. строки ниже.";
        return new AnnunciatorLampItem(
            EnvironmentReadinessCellIds.EnvSection,
            "Переменные окружения",
            detail,
            level,
            LampShortLabel: "ENV");
    }

    /// <summary>Worst of three env rows, но Advisory не зажигает сводную лампу: только Caution/Critical.</summary>
    internal static AnnunciatorLampLevel AggregateEnvBlockLevel(
        AnnunciatorLampLevel notes,
        AnnunciatorLampLevel canon,
        AnnunciatorLampLevel dbg) =>
        AggregateSectionLampLevelFromWorstChild(
            WorstAnnunciatorLevel(WorstAnnunciatorLevel(notes, canon), dbg));

    private static AnnunciatorLampLevel WorstAnnunciatorLevel(AnnunciatorLampLevel a, AnnunciatorLampLevel b) =>
        AnnunciatorLevelOrdinal(a) >= AnnunciatorLevelOrdinal(b) ? a : b;

    private static int AnnunciatorLevelOrdinal(AnnunciatorLampLevel l) => l switch
    {
        AnnunciatorLampLevel.Ok => 0,
        AnnunciatorLampLevel.Advisory => 1,
        AnnunciatorLampLevel.Caution => 2,
        AnnunciatorLampLevel.Critical => 3,
        _ => 0,
    };

    /// <summary>Для сводной лампы секции: Caution/Critical сохраняют уровень, Ok/Advisory — «норма» (лампа не горит).</summary>
    internal static AnnunciatorLampLevel AggregateSectionLampLevelFromWorstChild(AnnunciatorLampLevel worstChild) =>
        worstChild is AnnunciatorLampLevel.Caution or AnnunciatorLampLevel.Critical
            ? worstChild
            : AnnunciatorLampLevel.Ok;

    private static AnnunciatorLampItem BuildAgentNotesFileRow(string? raw)
    {
        const string title = WellKnownEnv.AgentNotesFile;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.AgentNotesFile,
                title,
                "Не задана: заметки в workspace/.cascade-ide/agent-notes.md при открытом решении; либо задай эту переменную для одного глобального файла.",
                AnnunciatorLampLevel.Ok,
                LampShortLabel: "Notes");
        }

        try
        {
            var full = Path.GetFullPath(raw.Trim());
            var parent = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
            {
                return new AnnunciatorLampItem(
                    EnvironmentReadinessCellIds.AgentNotesFile,
                    title,
                    "Задана: каталог для глобального файла заметок существует.",
                    AnnunciatorLampLevel.Ok,
                    LampShortLabel: "Notes");
            }

            if (File.Exists(full))
            {
                return new AnnunciatorLampItem(
                    EnvironmentReadinessCellIds.AgentNotesFile,
                    title,
                    "Задана: глобальный файл заметок существует.",
                    AnnunciatorLampLevel.Ok,
                    LampShortLabel: "Notes");
            }

            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.AgentNotesFile,
                title,
                "Родительский каталог для пути не найден — проверь AGENT_NOTES_FILE.",
                AnnunciatorLampLevel.Caution,
                LampShortLabel: "Notes");
        }
        catch
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.AgentNotesFile,
                title,
                "Некорректный путь в AGENT_NOTES_FILE.",
                AnnunciatorLampLevel.Critical,
                LampShortLabel: "Notes");
        }
    }

    private static AnnunciatorLampItem BuildAgentNotesCanonRow(string? raw)
    {
        const string title = WellKnownEnv.AgentNotesCanonPath;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.AgentNotesCanonPath,
                title,
                "Не задана: для knowledge передавай canon_path в MCP или задай корень репозитория agent-notes здесь.",
                AnnunciatorLampLevel.Advisory,
                LampShortLabel: "KB");
        }

        try
        {
            var full = Path.GetFullPath(raw.Trim());
            if (Directory.Exists(full))
            {
                return new AnnunciatorLampItem(
                    EnvironmentReadinessCellIds.AgentNotesCanonPath,
                    title,
                    "Каталог канона knowledge существует.",
                    AnnunciatorLampLevel.Ok,
                    LampShortLabel: "KB");
            }

            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.AgentNotesCanonPath,
                title,
                "Каталог не найден — проверь AGENT_NOTES_CANON_PATH.",
                AnnunciatorLampLevel.Caution,
                LampShortLabel: "KB");
        }
        catch
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.AgentNotesCanonPath,
                title,
                "Некорректный путь в AGENT_NOTES_CANON_PATH.",
                AnnunciatorLampLevel.Critical,
                LampShortLabel: "KB");
        }
    }

    private static AnnunciatorLampItem BuildNetcoreDbgRow(string? raw, Func<string?>? tryResolveNetcoreDbgWhenUnset = null)
    {
        const string title = WellKnownEnv.NetcoreDbgPath;
        if (string.IsNullOrWhiteSpace(raw))
        {
            var onPath = tryResolveNetcoreDbgWhenUnset is not null
                ? tryResolveNetcoreDbgWhenUnset.Invoke()
                : EnvironmentReadinessExecutablePathProbe.TryResolveExecutablePath("netcoredbg");
            if (onPath is not null)
            {
                return new AnnunciatorLampItem(
                    EnvironmentReadinessCellIds.NetcoreDbgPath,
                    title,
                    "Не задана: netcoredbg найден в PATH.",
                    AnnunciatorLampLevel.Ok,
                    LampShortLabel: "Dbg");
            }

            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.NetcoreDbgPath,
                title,
                "Не задана: netcoredbg в PATH не найден — задай NETCOREDBG_PATH или установи netcoredbg.",
                AnnunciatorLampLevel.Advisory,
                LampShortLabel: "Dbg");
        }

        var trimmed = raw.Trim();
        var resolved = EnvironmentReadinessExecutablePathProbe.TryResolveExecutablePath(trimmed);
        if (resolved is not null)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.NetcoreDbgPath,
                title,
                "Исполняемый файл найден (существующий путь или имя в PATH).",
                AnnunciatorLampLevel.Ok,
                LampShortLabel: "Dbg");
        }

        try
        {
            _ = Path.GetFullPath(trimmed);
        }
        catch
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.NetcoreDbgPath,
                title,
                "Некорректный путь в NETCOREDBG_PATH.",
                AnnunciatorLampLevel.Critical,
                LampShortLabel: "Dbg");
        }

        if (EnvironmentReadinessExecutablePathProbe.IsBareExecutableName(trimmed))
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.NetcoreDbgPath,
                title,
                "Имя без полного пути: в каталогах PATH исполняемый файл не найден.",
                AnnunciatorLampLevel.Caution,
                LampShortLabel: "Dbg");
        }

        return new AnnunciatorLampItem(
            EnvironmentReadinessCellIds.NetcoreDbgPath,
            title,
            "Файл по NETCOREDBG_PATH не найден.",
            AnnunciatorLampLevel.Caution,
            LampShortLabel: "Dbg");
    }

    /// <summary>Статическая часть: C# LSP, Markdown LSP (без сетевого вызова). Состояние — <see cref="IdeHostStateChanged"/> (тот же снимок, что на DataBus для IDE Health).</summary>
    public static IReadOnlyList<AnnunciatorLampItem> BuildLspRows(
        CascadeIdeSettings settings,
        string? solutionPath,
        in IdeHostStateChanged lsp)
    {
        var list = new List<AnnunciatorLampItem>(4);
        list.Add(BuildCSharpRow(settings, solutionPath, lsp.CSharpLspHostPresent, lsp.CSharpLspProcessActive));
        list.Add(BuildMarkdownRow(settings, solutionPath, lsp.MarkdownLspHostPresent, lsp.MarkdownLspProcessActive));
        return list;
    }

    /// <summary>Проверка <c>dotnet</c> в PATH (как при сборке).</summary>
    public static async Task<AnnunciatorLampItem> ProbeDotnetAsync(CancellationToken cancellationToken = default)
    {
        var r = await DotnetSdkVersionProbe.RunAsync(cancellationToken).ConfigureAwait(false);
        if (r.Outcome == DotnetSdkVersionProbeOutcome.Success && !string.IsNullOrWhiteSpace(r.Version))
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.DotnetSdk,
                "dotnet (SDK / CLI)",
                $"Версия: {r.Version}",
                AnnunciatorLampLevel.Ok,
                LampShortLabel: ".NET");
        }

        if (r.Outcome == DotnetSdkVersionProbeOutcome.ProcessNull)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.DotnetSdk,
                "dotnet (SDK / CLI)",
                "Не удалось запустить процесс dotnet.",
                AnnunciatorLampLevel.Critical,
                LampShortLabel: ".NET");
        }

        if (r.Outcome == DotnetSdkVersionProbeOutcome.Exception)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.DotnetSdk,
                "dotnet (SDK / CLI)",
                $"Не удалось выполнить dotnet --version: {r.ExceptionMessage ?? "unknown error"}",
                AnnunciatorLampLevel.Critical,
                LampShortLabel: ".NET");
        }

        // NonZeroExit
        var tail = string.IsNullOrWhiteSpace(r.StdErr) ? $"код выхода {r.ExitCode}" : r.StdErr;
        return new AnnunciatorLampItem(
            EnvironmentReadinessCellIds.DotnetSdk,
            "dotnet (SDK / CLI)",
            $"dotnet --version не удался ({tail}). Добавь dotnet в PATH или установи SDK.",
            AnnunciatorLampLevel.Critical,
            LampShortLabel: ".NET");
    }

    private static AnnunciatorLampItem BuildCSharpRow(
        CascadeIdeSettings settings,
        string? solutionPath,
        bool hostPresent,
        bool processActive)
    {
        var csharpLsp = settings.Languages.CSharp.ResolveForRuntime();
        var provider = csharpLsp.Mode;

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
            csharpLsp.Executable,
            csharpLsp.Arguments);

        var exeHint = string.IsNullOrWhiteSpace(exe) ? provider : $"{provider}: {exe}";

        if (!hostPresent)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.CSharpLsp,
                "C# LSP",
                $"{exeHint}. Процесс не запущен — проверь путь к исполняемому файлу и аргументы в настройках.",
                AnnunciatorLampLevel.Caution,
                LampShortLabel: "C#");
        }

        if (processActive)
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
        bool hostPresent,
        bool processActive)
    {
        var markdownLsp = settings.Languages.Markdown.ResolveForRuntime();
        var provider = markdownLsp.Mode;

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
            markdownLsp.Executable,
            markdownLsp.Arguments);

        var argHint = string.IsNullOrWhiteSpace(args) ? "" : $" {args}";
        var exeHint = string.IsNullOrWhiteSpace(exe) ? $"{provider}" : $"{provider}: {exe}{argHint}";

        if (!hostPresent)
        {
            return new AnnunciatorLampItem(
                EnvironmentReadinessCellIds.MarkdownLsp,
                "Markdown LSP",
                $"{exeHint}. Процесс не запущен — проверь установку (например Marksman в PATH) или путь в настройках.",
                AnnunciatorLampLevel.Caution,
                LampShortLabel: "MD");
        }

        if (processActive)
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
    /// Полный набор строк для страницы «готовность окружения» (ADR 0023): блок Dev Tools (агент, LSP, dotnet), затем env, затем переменные.
    /// Дополнительные проверки добавлять сюда, чтобы не раздувать ViewModel.
    /// </summary>
    public static async Task<IReadOnlyList<AnnunciatorLampItem>> BuildAllRowsAsync(
        CascadeIdeSettings settings,
        string? solutionPath,
        IdeHostStateChanged lsp,
        bool isMcpStdioHost = false,
        string? activeAiProvider = null,
        CancellationToken cancellationToken = default)
    {
        var agent = BuildAgentRow(isMcpStdioHost, activeAiProvider);
        var envRows = BuildEnvProbeRows(EnvironmentReadinessEnvSnapshot.FromCurrentProcess());
        var lspRows = BuildLspRows(settings, solutionPath, lsp);
        var dotnet = await ProbeDotnetAsync(cancellationToken).ConfigureAwait(false);

        var devToolDetails = new List<AnnunciatorLampItem>(1 + lspRows.Count + 1) { agent };
        devToolDetails.AddRange(lspRows);
        devToolDetails.Add(dotnet);

        var devToolsSection = BuildDevToolsSectionRow(devToolDetails);
        var envSection = BuildEnvSectionRow(envRows);

        var combined = new List<AnnunciatorLampItem>(devToolDetails.Count + envRows.Count + 2);
        combined.Add(devToolsSection);
        combined.AddRange(devToolDetails);
        combined.Add(envSection);
        combined.AddRange(envRows);
        return combined;
    }
}
