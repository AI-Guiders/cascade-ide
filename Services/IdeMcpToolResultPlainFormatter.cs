#nullable enable

using System.Text;
using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>
/// Превращает ответ MCP/IDE тула в читаемый текст для модели и для панели чата.
/// Снижает «подражание JSON» у локальных LLM и выглядит ближе к подписанным результатам тула в хостах вроде Cursor.
/// </summary>
internal static class IdeMcpToolResultPlainFormatter
{
    internal const int DefaultMaxCharsForModel = 14_000;
    internal const int DefaultMaxCharsForUiTrace = 2_800;
    internal const int DefaultMaxCharsForSalvagePayload = CascadeIdeMafIdeAgentChat.SalvageOutcomeMaxCharsForSummary;

    private const int MaxStringLeafChars = 600;
    private const int MaxObjectProperties = 80;
    private const int MaxArrayItems = 24;
    private const int MaxDepth = 14;

    /// <summary>Тело для возврата в цикл Agent Framework → модель (не сырой JSON при возможности).</summary>
    internal static string ForModel(string toolDisplayName, string outcome, int maxChars = DefaultMaxCharsForModel)
        => FormatCore(toolDisplayName, outcome, maxChars, forModel: true);

    /// <summary>Краткий текст трассы в истории чата («Инструмент»).</summary>
    internal static string ForUiTrace(string toolDisplayName, string outcome, int maxChars = DefaultMaxCharsForUiTrace)
        => FormatCore(toolDisplayName, outcome, maxChars, forModel: false);

    /// <summary>Текст в salvage user message — тоже не сырой JSON, чтобы второй проход модели не копировал скобки.</summary>
    internal static string ForSalvagePayload(string outcome, int maxChars = DefaultMaxCharsForSalvagePayload)
        => FormatCore("результат_инструмента", outcome, maxChars, forModel: true);

    private static string FormatCore(string toolDisplayName, string outcome, int maxChars, bool forModel)
    {
        outcome = outcome?.Trim() ?? "";
        if (outcome.Length == 0)
            return ClampLine($"Инструмент «{toolDisplayName}»: (пустой ответ)", maxChars);

        if (!LooksLikeStructuredJson(outcome))
        {
            var plain = $"Вызов: {toolDisplayName}\n{outcome}";
            if (forModel)
                plain = $"Инструмент «{toolDisplayName}» завершился.\n\n{outcome}";
            return ClampLine(plain, maxChars);
        }

        try
        {
            using var doc = JsonDocument.Parse(outcome);
            var sb = new StringBuilder();
            if (forModel)
            {
                sb.AppendLine($"Инструмент «{toolDisplayName}» завершился.");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"Вызов: {toolDisplayName}");
                sb.AppendLine(new string('─', Math.Min(32, maxChars > 40 ? 32 : 8)));
            }

            AppendElement(sb, doc.RootElement, depth: 0, linesBudget: forModel ? 220 : 80);
            return ClampLine(sb.ToString().TrimEnd(), maxChars);
        }
        catch (JsonException)
        {
            var plain = forModel
                ? $"Инструмент «{toolDisplayName}» завершился.\n\n{outcome}"
                : $"Вызов: {toolDisplayName}\n{outcome}";
            return ClampLine(plain, maxChars);
        }
    }

    private static bool LooksLikeStructuredJson(string s)
    {
        if (s.Length < 2)
            return false;
        var c = s[0];
        return c is '{' or '[';
    }

    private static void AppendElement(StringBuilder sb, JsonElement el, int depth, int linesBudget)
    {
        var ind = IndentString(depth);
        if (depth > MaxDepth || linesBudget <= 0)
        {
            sb.Append(ind).AppendLine("…");
            return;
        }

        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var n = 0;
                foreach (var p in el.EnumerateObject())
                {
                    if (n >= MaxObjectProperties || linesBudget <= 0)
                    {
                        sb.Append(ind).AppendLine("… (ещё поля опущены)");
                        return;
                    }

                    sb.Append(ind).Append(p.Name).Append(": ");
                    if (p.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        sb.AppendLine();
                        AppendElement(sb, p.Value, depth + 1, linesBudget - 1);
                    }
                    else
                    {
                        AppendInlinePrimitive(sb, p.Value);
                        sb.AppendLine();
                    }

                    n++;
                    linesBudget--;
                }

                break;
            }
            case JsonValueKind.Array:
            {
                var len = el.GetArrayLength();
                sb.Append(ind).Append("[массив, элементов: ").Append(len).AppendLine("]");
                var i = 0;
                foreach (var item in el.EnumerateArray())
                {
                    var indInner = IndentString(depth + 1);
                    if (i >= MaxArrayItems || linesBudget <= 0)
                    {
                        sb.Append(indInner).AppendLine("…");
                        return;
                    }

                    sb.Append(indInner).Append('[').Append(i).Append(']').Append(' ');
                    if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        sb.AppendLine();
                        AppendElement(sb, item, depth + 2, linesBudget - 2);
                    }
                    else
                    {
                        AppendInlinePrimitive(sb, item);
                        sb.AppendLine();
                    }

                    i++;
                    linesBudget--;
                }

                break;
            }
            default:
                AppendInlinePrimitive(sb, el);
                sb.AppendLine();
                break;
        }
    }

    private static void AppendInlinePrimitive(StringBuilder sb, JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
            {
                var t = el.GetString() ?? "";
                if (t.Length > MaxStringLeafChars)
                    sb.Append(t.AsSpan(0, MaxStringLeafChars)).Append("… (").Append(t.Length).Append(" симв.)");
                else
                    sb.Append(t);
                break;
            }
            case JsonValueKind.Number:
                sb.Append(el.GetRawText());
                break;
            case JsonValueKind.True:
                sb.Append("да");
                break;
            case JsonValueKind.False:
                sb.Append("нет");
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                sb.Append("(нет)");
                break;
            default:
                sb.Append(el.GetRawText());
                break;
        }
    }

    private static string IndentString(int depth)
    {
        var n = Math.Clamp(depth * 2, 0, 64);
        return n == 0 ? "" : new string(' ', n);
    }

    private static string ClampLine(string s, int maxChars)
    {
        if (s.Length <= maxChars)
            return s;
        return s.AsSpan(0, Math.Max(0, maxChars - 80)).ToString()
               + $"\n… (всего {s.Length} симв.; показано ~{maxChars - 80}.)";
    }
}
