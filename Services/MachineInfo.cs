using System.Runtime.InteropServices;

namespace CascadeIDE.Services;

/// <summary>Рекомендации моделей Ollama по конфигурации машины (RAM).</summary>
public static class MachineInfo
{
    /// <summary>Объём физической памяти в МБ (0 если не удалось определить).</summary>
    public static ulong GetTotalPhysicalMemoryMb()
    {
        if (OperatingSystem.IsWindows())
            return GetTotalPhysicalMemoryMbWindows();
        // Linux/macOS: можно добавить парсинг /proc/meminfo или sysctl
        return 0;
    }

    private static ulong GetTotalPhysicalMemoryMbWindows()
    {
        try
        {
            var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref mem))
                return mem.ullTotalPhys / (1024 * 1024);
        }
        catch
        {
            // ignore
        }
        return 0;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    /// <summary>Рекомендуемые модели для установки (под кодинг + MCP). Группа по RAM.</summary>
    public static IReadOnlyList<Models.RecommendedModel> GetRecommendedModels()
    {
        var ramMb = GetTotalPhysicalMemoryMb();
        // Нет данных — считаем средний ПК (16 GB)
        if (ramMb == 0) ramMb = 16 * 1024;

        return ramMb switch
        {
            < 10 * 1024 => LightTier,   // < 10 GB
            < 24 * 1024 => MediumTier, // 10–24 GB (ноутбук)
            _ => HeavyTier             // 24+ GB
        };
    }

    private static readonly List<Models.RecommendedModel> LightTier =
    [
        new("qwen2.5-coder:3b", "Лёгкая модель для кода (3B)"),
        new("phi4:2.5b", "Компактная универсальная (2.5B)"),
        new("qwen2.5:1.5b", "Минимальные требования (1.5B)")
    ];

    private static readonly List<Models.RecommendedModel> MediumTier =
    [
        new("qwen2.5-coder:7b", "Для кода и MCP (7B) — рекомендуется"),
        new("llama3.1:8b-instruct", "Универсальная, tool calling (8B)"),
        new("qwen2.5-coder:3b", "Лёгкая для кода (3B)"),
        new("mistral:7b-instruct", "Быстрая (7B)")
    ];

    private static readonly List<Models.RecommendedModel> HeavyTier =
    [
        new("qwen2.5-coder:14b", "Мощная для кода (14B)"),
        new("qwen2.5-coder:7b", "Для кода и MCP (7B)"),
        new("llama3.1:8b-instruct", "Универсальная (8B)"),
        new("qwen2.5-coder:32b", "Максимум качества (32B)")
    ];
}

