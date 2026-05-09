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
            ? CanonicalFilePath.Normalize(expanded)
            : CanonicalFilePath.Normalize(Path.Combine(UserSettingsPaths.GetSettingsDirectory(), expanded));

        var fromFile = TextFileReadWrite.TryReadAllTextIfExists(fullPath);
        if (fromFile is not null)
            return fromFile;

        return settings.Mcp.ExternalServersJson ?? "[]";
    }
}
