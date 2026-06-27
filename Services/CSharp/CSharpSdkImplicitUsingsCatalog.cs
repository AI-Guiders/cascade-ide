namespace CascadeIDE.Services;

/// <summary>SDK implicit namespace imports (Microsoft.NET.Sdk / Web).</summary>
public static class CSharpSdkImplicitUsingsCatalog
{
    private static readonly string[] NetSdk =
    [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
    ];

    private static readonly string[] WebSdk =
    [
        "System.Net.Http.Json",
        "Microsoft.AspNetCore.Builder",
        "Microsoft.AspNetCore.Hosting",
        "Microsoft.AspNetCore.Http",
        "Microsoft.AspNetCore.Routing",
        "Microsoft.Extensions.Configuration",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Hosting",
        "Microsoft.Extensions.Logging",
    ];

    public static IReadOnlyList<string> ForProjectSdk(string? sdkAttribute, bool implicitUsingsEnabled)
    {
        if (!implicitUsingsEnabled)
            return [];

        var list = new List<string>(NetSdk);
        if (SdkIncludesWeb(sdkAttribute))
            list.AddRange(WebSdk);

        return list;
    }

    private static bool SdkIncludesWeb(string? sdkAttribute)
    {
        if (string.IsNullOrWhiteSpace(sdkAttribute))
            return false;

        foreach (var part in sdkAttribute.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
