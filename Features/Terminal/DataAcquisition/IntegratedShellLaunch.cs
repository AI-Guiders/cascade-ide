using System.Diagnostics;
using System.Text;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Terminal.DataAcquisition;

/// <summary>
/// Выбор shell и фабрика PTY-сессии (ConPTY на Windows, redirected fallback).
/// Адаптировано из MIT-примеров AvaloniaTerminal.
/// </summary>
[IoBoundary]
internal static class IntegratedShellLaunch
{
    public static string ResolveWorkingDirectory(string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return Environment.CurrentDirectory;
        try
        {
            var p = CanonicalFilePath.Normalize(solutionPath.Trim());
            if (File.Exists(p))
                return Path.GetDirectoryName(p) ?? Environment.CurrentDirectory;
            if (Directory.Exists(p))
                return p;
        }
        catch
        {
            // fall through
        }

        return Environment.CurrentDirectory;
    }

    public static ShellLaunchConfiguration ResolveLaunchConfiguration(string workingDirectory)
    {
        foreach (var candidate in EnumerateLaunchCandidates(workingDirectory))
            return candidate;

        var cwd = NormalizeExistingWorkingDirectory(workingDirectory);
        return new ShellLaunchConfiguration("echo", ["No shell found"], "echo", cwd);
    }

    public static IEnumerable<ShellLaunchConfiguration> EnumerateLaunchCandidates(string workingDirectory)
    {
        var cwd = NormalizeExistingWorkingDirectory(workingDirectory);

        if (OperatingSystem.IsWindows())
        {
            if (ResolvePwshExecutable() is { } pwsh)
                yield return new ShellLaunchConfiguration(pwsh, PwshLaunchArguments, "pwsh.exe", cwd);

            var commandPrompt = Environment.GetEnvironmentVariable("ComSpec");
            if (!string.IsNullOrWhiteSpace(commandPrompt) && File.Exists(commandPrompt))
                yield return new ShellLaunchConfiguration(commandPrompt, CmdLaunchArguments, Path.GetFileName(commandPrompt), cwd);

            if (FindExecutableInPath("cmd.exe") is { } cmd)
                yield return new ShellLaunchConfiguration(cmd, CmdLaunchArguments, "cmd.exe", cwd);

            yield break;
        }

        yield return new ShellLaunchConfiguration("echo", ["No shell found"], "echo", cwd);
    }

    /// <summary>UTF-8 для ConPTY + xterm: cmd по умолчанию CP866/OEM, эмулятор ждёт UTF-8.</summary>
    internal static void ApplyUtf8ConsoleEnvironment(IDictionary<string, string> environmentVariables)
    {
        environmentVariables["LANG"] = "ru_RU.UTF-8";
        environmentVariables["LC_ALL"] = "ru_RU.UTF-8";
        environmentVariables["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
    }

    internal static void ApplyUtf8ConsoleEnvironment(System.Collections.Specialized.StringDictionary environment)
    {
        environment["LANG"] = "ru_RU.UTF-8";
        environment["LC_ALL"] = "ru_RU.UTF-8";
        environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
    }

    private static readonly string[] PwshLaunchArguments =
    [
        "-NoLogo",
        "-NoExit",
        "-Command",
        "[Console]::InputEncoding=[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false); chcp 65001 | Out-Null",
    ];

    private static readonly string[] CmdLaunchArguments = ["/K", "chcp 65001>nul"];

    public static IIntegratedShellSession CreateSession(ShellLaunchConfiguration launch)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var conPty = new WindowsConPtyIntegratedShellSession(launch);
                conPty.Start();
                return conPty;
            }
            catch (Exception ex) when (ShouldFallbackFromConPty(ex))
            {
            }
        }

        var redirected = new RedirectedIntegratedShellSession(launch);
        redirected.Start();
        return redirected;
    }

    internal static string NormalizeExistingWorkingDirectory(string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
            return Environment.CurrentDirectory;

        try
        {
            var full = Path.GetFullPath(workingDirectory.Trim());
            if (Directory.Exists(full))
                return full;
        }
        catch
        {
            // fall through
        }

        return Environment.CurrentDirectory;
    }

    public static byte[] NormalizeStandardInput(byte[] input)
    {
        if (input.Length == 0)
            return input;

        var normalized = new List<byte>(input.Length + 4);
        var newline = Encoding.UTF8.GetBytes(Environment.NewLine);

        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];
            if (current == '\r')
            {
                normalized.AddRange(newline);
                if (i + 1 < input.Length && input[i + 1] == '\n')
                    i++;
                continue;
            }

            if (current == '\n')
            {
                normalized.AddRange(newline);
                continue;
            }

            normalized.Add(current);
        }

        return normalized.ToArray();
    }

    internal static bool ShouldApplyResize((int cols, int rows)? lastResize, int cols, int rows, out (int cols, int rows) normalized)
    {
        normalized = (Math.Max(cols, 1), Math.Max(rows, 1));
        return lastResize != normalized;
    }

    private static bool ShouldFallbackFromConPty(Exception ex) =>
        ex is PlatformNotSupportedException
            or EntryPointNotFoundException
            or DllNotFoundException
            or InvalidOperationException
            or System.ComponentModel.Win32Exception
            or FileNotFoundException;

    private static string? ResolvePwshExecutable()
    {
        if (FindExecutableInPath("pwsh.exe") is { } fromPath)
            return fromPath;

        foreach (var candidate in PwshWellKnownPaths)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static readonly string[] PwshWellKnownPaths =
    [
        @"C:\Program Files\PowerShell\7\pwsh.exe",
        @"C:\Program Files\PowerShell\7-preview\pwsh.exe",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
    ];

    private static string? FindExecutableInPath(string command)
    {
        if (Path.IsPathRooted(command)
            || command.Contains(Path.DirectorySeparatorChar)
            || command.Contains(Path.AltDirectorySeparatorChar))
        {
            return File.Exists(command) ? command : null;
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return null;

        IEnumerable<string> extensions = [string.Empty];
        if (OperatingSystem.IsWindows())
        {
            var pathExt = Environment.GetEnvironmentVariable("PATHEXT");
            if (!string.IsNullOrWhiteSpace(pathExt))
            {
                extensions = pathExt.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(
                    directory,
                    command.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? command : command + extension);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }
}
