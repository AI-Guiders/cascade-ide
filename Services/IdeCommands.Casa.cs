namespace CascadeIDE.Services;

/// <summary>CASA field query → KB section navigation (ontology-in-field IDE bridge v1).</summary>
public static partial class IdeCommands
{
    /// <summary>CASA field query: match KB concepts → doc_path + section. args: query:string, open?:boolean; returns: json; example: {"query":"import knowledge delta","open":true}.</summary>
    public const string CasaFieldQuery = "casa_field_query";
}
