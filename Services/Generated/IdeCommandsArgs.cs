namespace CascadeIDE.Services;

internal static class IdeCommandsArgs
{
    internal readonly record struct Arg(string Name, string JsonType, bool Required, bool IsArray, string? ItemJsonType);

    public static bool TryGetArgs(string commandId, out Arg[] args)
    {
        // Keep the main build independent from doc generation.
        // When docs generation is enabled, tools/CascadeIDE.ProtocolDocGen produces IdeCommandsArgsGenerated
        // which we locate via reflection.
        try
        {
            var t = Type.GetType("CascadeIDE.Services.IdeCommandsArgsGenerated, CascadeIDE", throwOnError: false);
            if (t is null)
            {
                args = Array.Empty<Arg>();
                return false;
            }

            var m = t.GetMethod("TryGetArgs", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (m is null)
            {
                args = Array.Empty<Arg>();
                return false;
            }

            object?[] invokeArgs = [commandId, null!];
            var okObj = m.Invoke(null, invokeArgs);
            if (okObj is bool ok && ok && invokeArgs[1] is Arg[] arr)
            {
                args = arr;
                return true;
            }

            args = invokeArgs[1] as Arg[] ?? Array.Empty<Arg>();
            return okObj is bool ok2 && ok2;
        }
        catch
        {
            args = Array.Empty<Arg>();
            return false;
        }
    }
}

internal enum IdeReturnKind
{
    Unspecified = 0,
    Text = 1,
    Json = 2,
    None = 3
}

internal static class IdeCommandsContract
{
    public static bool TryGetReturns(string commandId, out IdeReturnKind kind)
    {
        kind = IdeReturnKind.Unspecified;
        try
        {
            var t = Type.GetType("CascadeIDE.Services.IdeCommandsContractGenerated, CascadeIDE", throwOnError: false);
            if (t is null)
                return false;

            var m = t.GetMethod("TryGetReturns", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (m is null)
                return false;

            object?[] invokeArgs = [commandId, null];
            var okObj = m.Invoke(null, invokeArgs);
            if (okObj is bool ok && ok && invokeArgs[1] is IdeReturnKind rk)
            {
                kind = rk;
                return true;
            }

            if (invokeArgs[1] is IdeReturnKind rk2)
                kind = rk2;

            return okObj is bool ok2 && ok2;
        }
        catch
        {
            kind = IdeReturnKind.Unspecified;
            return false;
        }
    }

    public static bool TryGetExample(string commandId, out string example)
    {
        example = "";
        try
        {
            var t = Type.GetType("CascadeIDE.Services.IdeCommandsContractGenerated, CascadeIDE", throwOnError: false);
            if (t is null)
                return false;

            var m = t.GetMethod("TryGetExample", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (m is null)
                return false;

            object?[] invokeArgs = [commandId, null];
            var okObj = m.Invoke(null, invokeArgs);
            if (okObj is bool ok && ok && invokeArgs[1] is string s)
            {
                example = s;
                return true;
            }

            example = invokeArgs[1] as string ?? "";
            return okObj is bool ok2 && ok2;
        }
        catch
        {
            example = "";
            return false;
        }
    }
}

