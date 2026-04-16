#nullable enable
using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Services.AgentContract;

/// <summary>
/// Headless вывод JSON контракта агента в stdout (ADR 0052): те же сборщики, что и MCP <c>ide_*</c>, без stdio MCP.
/// Запуск: <c>--agent-contract &lt;command&gt;</c> (см. <see cref="PrintHelp"/>).
/// </summary>
public static class AgentContractRunner
{
    /// <summary>Возвращает код выхода: 0 — ок, 1 — help/нет команды, 2 — неизвестная команда.</summary>
    public static int Run(ReadOnlySpan<string> args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return args.Length == 0 ? 1 : 0;
        }

        if (!TryGetJson(args[0], out var json, out var error))
        {
            Console.Error.WriteLine(error);
            return 2;
        }

        Console.Out.WriteLine(json);
        return 0;
    }

    /// <summary>Тот же JSON, что вернёт MCP для соответствующей команды (для тестов и внешних вызовов).</summary>
    public static string GetContractJson(string command)
    {
        if (!TryGetJson(command, out var json, out var error) || json is null)
            throw new InvalidOperationException(error ?? "Unknown command.");

        return json;
    }

    public static bool TryGetJson(string command, [NotNullWhen(true)] out string? json, [NotNullWhen(false)] out string? error)
    {
        json = null;
        error = null;

        switch (command)
        {
            case global::CascadeIDE.Services.IdeCommands.GetUiModesDiagnostics:
                UiModeCatalog.Initialize();
                json = UiModeCatalog.GetDiagnosticsJson();
                return true;
            default:
                error = $"Unknown agent contract command: {command}. See --agent-contract --help.";
                return false;
        }
    }

    private static bool IsHelp(string arg) =>
        string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
        || string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase);

    private static void PrintHelp()
    {
        Console.Out.WriteLine(
            """
            CascadeIDE --agent-contract <command>
            Print the same JSON as the matching ide_* MCP tool (ADR 0052). No GUI; no MCP stdio.

            Commands:
              get_ui_modes_diagnostics   Same payload as ide_get_ui_modes_diagnostics

            Example:
              CascadeIDE.exe --agent-contract get_ui_modes_diagnostics
            """);
    }
}
