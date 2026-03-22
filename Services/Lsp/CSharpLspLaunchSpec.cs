namespace CascadeIDE.Services.Lsp;

/// <summary>Пресеты запуска C# language server (один активный процесс).</summary>
public static class CSharpLspProviderIds
{
    public const string ParseOnly = "ParseOnly";
    public const string OmniSharp = "OmniSharp";
    public const string CSharpLs = "CSharpLs";
    public const string Custom = "Custom";

    public static readonly string[] All = [ParseOnly, OmniSharp, CSharpLs, Custom];

    public static (string fileName, string arguments) ResolveProcessArgs(string providerId, string? solutionPath, string? userExecutable, string? userArguments)
    {
        var extra = string.IsNullOrWhiteSpace(userArguments) ? "" : userArguments.Trim();
        return providerId switch
        {
            CSharpLs => (
                string.IsNullOrWhiteSpace(userExecutable) ? "csharp-ls" : userExecutable.Trim(),
                extra),
            Custom => (
                userExecutable?.Trim() ?? "dotnet",
                extra),
            OmniSharp => (
                string.IsNullOrWhiteSpace(userExecutable) ? "OmniSharp" : userExecutable.Trim(),
                CombineArgs("--languageserver", extra)),
            _ => ("", "")
        };
    }

    private static string CombineArgs(string preset, string extra) =>
        string.IsNullOrEmpty(extra) ? preset : $"{preset} {extra}";
}
