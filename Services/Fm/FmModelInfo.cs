namespace CascadeIDE.Services.Fm;

/// <summary>Кэшированная запись <c>GET /v1/models</c> (Cloud.ru FM и др. OpenAI-compatible).</summary>
public sealed record FmModelInfo(
    string ModelId,
    int? MaxModelLen,
    double? PromptTokensCost,
    double? GeneratedTokensCost);
