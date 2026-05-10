using System.Text.Json;

namespace CascadeIDE.Features.WebAiPortal.Application;

/// <summary>
/// Вытащить последнюю команду (<c>command_id</c> + args) со страницы чата вниз по порядку сбора.
/// Учитывает: fenced ```json-cascade; голый языковой маркер <c>json-cascade</c> + перевод строки + JSON (частый вывод Gemini без backticks); <c>pre</c>/<code>;
/// дополнительно тот же поиск по <see cref="innerText"/> корня документа (блок может жить не в pre/code).
/// </summary>
public static class WebAiPortalLastCommandDomProbe
{
    /// <summary>IIFE возвращает JSON-строку вида один объект или пустую строку (результат WebView сериализуется).</summary>
    internal const string LastPayloadProbeScriptJavaScript =
        """
        (() => {
          function extractFirstJsonObject(text) {
            const i = text.indexOf('{');
            if (i < 0) return null;
            let depth = 0;
            let inStr = false;
            let esc = false;
            for (let j = i; j < text.length; j++) {
              const c = text[j];
              if (inStr) {
                if (esc) { esc = false; continue; }
                if (c === '\\') { esc = true; continue; }
                if (c === '"') { inStr = false; continue; }
                continue;
              }
              if (c === '"') { inStr = true; continue; }
              if (c === '{') depth++;
              else if (c === '}') {
                depth--;
                if (depth === 0) return text.slice(i, j + 1);
              }
            }
            return null;
          }

          function tryPushCommandJson(raw, arr) {
            if (!raw) return;
            let s = String(raw).trim();

            const fm = s.match(/```json-cascade\s*\r?\n([\s\S]*?)```/);
            if (fm) s = fm[1].trim();
            else if (/^json-cascade\s*\r?\n/i.test(s))
              s = s.replace(/^json-cascade\s*\r?\n/i, '').trim();

            if (!s.startsWith('{')) return;
            if (s.indexOf('"command_id"') < 0) return;
            try {
              const cand = extractFirstJsonObject(s) ?? s;
              const o = JSON.parse(cand);
              if (typeof o.command_id === 'string' && o.command_id)
                arr.push(JSON.stringify(o));
            } catch (e) {}
          }

          function collectDomElements(arr) {
            document.querySelectorAll('pre').forEach(el =>
              tryPushCommandJson(el.textContent, arr));
            document.querySelectorAll('code').forEach(el => {
              if (!el.closest('pre'))
                tryPushCommandJson(el.textContent, arr);
            });
          }

          function collectFromPlainText(chunk, arr) {
            if (!chunk) return;
            const reFence = /```json-cascade\s*\r?\n?([\s\S]*?)```/gi;
            let m;
            while ((m = reFence.exec(chunk)) !== null)
              tryPushCommandJson(m[1], arr);

            const needle = 'json-cascade';
            const lower = chunk;
            const low = lower.toLowerCase();
            for (let i = 0; i < low.length;) {
              const idx = low.indexOf(needle, i);
              if (idx < 0) break;
              const before = idx === 0 ? ' ' : lower[idx - 1];
              const afterPos = idx + needle.length;
              const after = afterPos < lower.length ? lower[afterPos] : ' ';
              const wordish = /[0-9A-Za-z_]/;
              if (!wordish.test(before) && !wordish.test(after))
                tryPushCommandJson(lower.slice(idx), arr);
              i = idx + 1;
            }
          }

          const payloads = [];
          collectDomElements(payloads);
          const bodyText = document.body ? document.body.innerText : '';
          collectFromPlainText(bodyText, payloads);

          return payloads.length ? payloads[payloads.length - 1] : '';
        })()
        """;

    /// <summary>Снять одну оболочку JSON-string с результата <see cref="Avalonia.Controls.NativeWebView.InvokeScript"/> (WebView2).</summary>
    public static string? UnwrapWrappedJsonString(string? invokeScriptResult)
    {
        if (string.IsNullOrWhiteSpace(invokeScriptResult))
            return invokeScriptResult?.Trim();
        var t = invokeScriptResult.Trim();
        if (t.Length >= 2 && t[0] == '"' && t[^1] == '"')
        {
            try
            {
                return JsonSerializer.Deserialize<string>(t);
            }
            catch (JsonException)
            {
                return t;
            }
        }

        return t;
    }
}
