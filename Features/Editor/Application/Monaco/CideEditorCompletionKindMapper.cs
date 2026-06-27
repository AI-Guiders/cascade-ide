using CascadeIDE.Services;

namespace CascadeIDE.Features.Editor.Application.Monaco;

public static class CideEditorCompletionKindMapper
{
    public static string? FromRoslyn(CSharpLanguageService.CSharpCompletionKind kind) =>
        kind switch
        {
            CSharpLanguageService.CSharpCompletionKind.Keyword => "keyword",
            CSharpLanguageService.CSharpCompletionKind.Method => "method",
            CSharpLanguageService.CSharpCompletionKind.Property => "property",
            CSharpLanguageService.CSharpCompletionKind.Field => "field",
            CSharpLanguageService.CSharpCompletionKind.Event => "event",
            CSharpLanguageService.CSharpCompletionKind.EnumMember => "enumMember",
            CSharpLanguageService.CSharpCompletionKind.Enum => "enum",
            CSharpLanguageService.CSharpCompletionKind.Class => "class",
            CSharpLanguageService.CSharpCompletionKind.Interface => "interface",
            CSharpLanguageService.CSharpCompletionKind.Struct => "struct",
            CSharpLanguageService.CSharpCompletionKind.Delegate => "delegate",
            CSharpLanguageService.CSharpCompletionKind.Variable => "variable",
            _ => "text",
        };

    public static string? FromLspKind(int? kind) =>
        kind switch
        {
            1 => "text",
            2 => "method",
            3 => "function",
            4 => "constructor",
            5 => "field",
            6 => "variable",
            7 => "class",
            8 => "interface",
            9 => "module",
            10 => "property",
            11 => "unit",
            12 => "value",
            13 => "enum",
            14 => "keyword",
            15 => "snippet",
            16 => "color",
            17 => "file",
            18 => "reference",
            19 => "folder",
            20 => "enumMember",
            21 => "constant",
            22 => "struct",
            23 => "event",
            24 => "operator",
            25 => "typeParameter",
            _ => null,
        };
}
