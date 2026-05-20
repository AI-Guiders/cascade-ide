#nullable enable
using System.Diagnostics;
using System.Globalization;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Трассировка отправки Intercom (attach @ send, провайдер). Файл: <c>{workspace}/.cascade-ide/intercom-send-trace.log</c>.
/// </summary>
/// <remarks>
/// Включение: <c>%LocalAppData%\CascadeIDE\settings.toml</c> — <c>[logging.intercom] send_trace = true</c> (по умолчанию false).
/// Границы фаз — через <see cref="Run"/>, <see cref="RunAsync"/> и <see cref="IntercomSendPhase.Detail"/>.
/// </remarks>
public static class IntercomSendTrace
{
    private const int MaxLogBytes = 512 * 1024;

    private static bool _settingsCacheValid;
    private static bool _settingsSendTrace;

    public static bool IsEnabled => ReadSettingsSendTrace();

    /// <summary>Сбросить кэш после <see cref="SettingsService.Load"/> / <see cref="SettingsService.Save"/>.</summary>
    public static void InvalidateSettingsCache() => _settingsCacheValid = false;

    public static T Run<T>(string? workspaceRoot, string phase, Func<IntercomSendPhase, T> body)
    {
        using var _ = Begin(workspaceRoot, phase);
        return body(new IntercomSendPhase(workspaceRoot, phase));
    }

    public static void Run(string? workspaceRoot, string phase, Action<IntercomSendPhase> body)
    {
        using var _ = Begin(workspaceRoot, phase);
        body(new IntercomSendPhase(workspaceRoot, phase));
    }

    public static async Task RunAsync(string? workspaceRoot, string phase, Func<IntercomSendPhase, Task> body)
    {
        using var _ = Begin(workspaceRoot, phase);
        await body(new IntercomSendPhase(workspaceRoot, phase)).ConfigureAwait(false);
    }

    public static async Task<T> RunAsync<T>(
        string? workspaceRoot,
        string phase,
        Func<IntercomSendPhase, Task<T>> body)
    {
        using var _ = Begin(workspaceRoot, phase);
        return await body(new IntercomSendPhase(workspaceRoot, phase)).ConfigureAwait(false);
    }

    public static IDisposable Begin(string? workspaceRoot, string phase) =>
        IsEnabled ? new Phase(workspaceRoot, phase) : NoopPhase.Instance;

    public static void Write(string? workspaceRoot, string phase, string detail)
    {
        if (!IsEnabled)
            return;

        var line = $"[{UtcStamp()}] {phase} {detail}";
        System.Diagnostics.Debug.WriteLine("[intercom-send] " + line);
        TryAppendFile(workspaceRoot, line);
    }

    private static bool ReadSettingsSendTrace()
    {
        if (_settingsCacheValid)
            return _settingsSendTrace;

        _settingsCacheValid = true;
        try
        {
            _settingsSendTrace = SettingsService.Load().Logging.Intercom.SendTrace;
        }
        catch
        {
            _settingsSendTrace = false;
        }

        return _settingsSendTrace;
    }

    private static string UtcStamp() =>
        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "Z";

    private static void TryAppendFile(string? workspaceRoot, string line)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return;

        try
        {
            var root = workspaceRoot.Trim();
            if (!Directory.Exists(root))
                return;

            var logDir = Path.Combine(root, ".cascade-ide");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "intercom-send-trace.log");
            trimLogIfNeeded(logPath);
            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch
        {
            // best-effort
        }
    }

    private static void trimLogIfNeeded(string logPath)
    {
        try
        {
            if (!File.Exists(logPath))
                return;

            var info = new FileInfo(logPath);
            if (info.Length <= MaxLogBytes)
                return;

            var keepBytes = MaxLogBytes / 2;
            using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream.Seek(-keepBytes, SeekOrigin.End);
            var tail = new byte[keepBytes];
            _ = stream.Read(tail, 0, keepBytes);
            File.WriteAllBytes(logPath, tail);
        }
        catch
        {
            // best-effort
        }
    }

    private sealed class Phase : IDisposable
    {
        private readonly string? _workspaceRoot;
        private readonly string _phase;
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public Phase(string? workspaceRoot, string phase)
        {
            _workspaceRoot = workspaceRoot;
            _phase = phase;
            Write(workspaceRoot, phase, "start");
        }

        public void Dispose()
        {
            _sw.Stop();
            Write(_workspaceRoot, _phase, $"done elapsed_ms={_sw.ElapsedMilliseconds}");
        }
    }

    private sealed class NoopPhase : IDisposable
    {
        public static readonly NoopPhase Instance = new();
        public void Dispose() { }
    }
}

/// <summary>Контекст активной фазы трассировки (детали внутри <see cref="IntercomSendTrace.Run"/>).</summary>
public readonly struct IntercomSendPhase
{
    private readonly string? _workspaceRoot;
    private readonly string _phase;

    internal IntercomSendPhase(string? workspaceRoot, string phase)
    {
        _workspaceRoot = workspaceRoot;
        _phase = phase;
    }

    public void Detail(string detail) => IntercomSendTrace.Write(_workspaceRoot, _phase, detail);
}
