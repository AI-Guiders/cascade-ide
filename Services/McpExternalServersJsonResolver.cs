using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>
/// Подставляет JSON внешних MCP: при непустом пути и существующем файле — содержимое файла, иначе inline из настроек.
/// </summary>
public static class McpExternalServersJsonResolver
{
    /// <summary>
    /// Эффективная строка JSON (массив спецификаций серверов) для <see cref="McpClientService"/> и Cursor ACP.
    /// </summary>
    public static string ResolveEffectiveJson(CascadeIdeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var path = settings.Mcp.ExternalServersJsonPath?.Trim() ?? "";
        if (path.Length == 0)
            return settings.Mcp.ExternalServersJson ?? "[]";

        var expanded = Environment.ExpandEnvironmentVariables(path);
        var fullPath = Path.IsPathRooted(expanded)
            ? Path.GetFullPath(expanded)
            : Path.GetFullPath(Path.Combine(SettingsService.GetSettingsDirectory(), expanded));

        try
        {
            if (File.Exists(fullPath))
                return File.ReadAllText(fullPath);
        }
        catch
        {
            // Файл недоступен — fallback на inline.
        }

        return settings.Mcp.ExternalServersJson ?? "[]";
    }
}
