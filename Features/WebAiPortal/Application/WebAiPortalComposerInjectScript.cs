using System.Text;
using System.Text.Json;

namespace CascadeIDE.Features.WebAiPortal.Application;

/// <summary>
/// Строит JavaScript для вставки UTF-8 текста в активный <c>textarea</c>/<c>input</c> или contenteditable после ответа моста ADR 0108 (эвристика сторонней страницы).
/// </summary>
public static class WebAiPortalComposerInjectScript
{
    /// <summary>Возвращает IIFE со строкой JSON с полями <c>ok</c>, <c>reason</c>, опционально <c>tag</c>.</summary>
    public static string Build(ReadOnlySpan<char> utf16Text)
    {
        var utf8Count = Encoding.UTF8.GetByteCount(utf16Text);
        Span<byte> utf8 = utf8Count <= 1024 ? stackalloc byte[utf8Count] : new byte[utf8Count];
        _ = Encoding.UTF8.GetBytes(utf16Text, utf8);

        var b64 = Convert.ToBase64String(utf8);
        var b64Js = JsonSerializer.Serialize(b64);

        return """
(() => {
  const b64 = %%B64%%;
  let t;
  try {
    const bin = atob(b64);
    const bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    t = new TextDecoder("utf-8").decode(bytes);
  } catch (e) {
    return JSON.stringify({ ok: false, reason: "decode" });
  }
  const sep = "\n\n";
  const ins = sep + t;
  const el = document.activeElement;
  if (!el) return JSON.stringify({ ok: false, reason: "no_focus" });
  const tag = el.tagName || "";
  if (tag === "TEXTAREA") {
    const ta = /** @type {HTMLTextAreaElement} */ (el);
    const start = typeof ta.selectionStart === "number" ? ta.selectionStart : ta.value.length;
    const end = typeof ta.selectionEnd === "number" ? ta.selectionEnd : ta.value.length;
    ta.value = ta.value.slice(0, start) + ins + ta.value.slice(end);
    const pos = start + ins.length;
    ta.selectionStart = ta.selectionEnd = pos;
    ta.dispatchEvent(new Event("input", { bubbles: true }));
    ta.dispatchEvent(new Event("change", { bubbles: true }));
    return JSON.stringify({ ok: true, tag: "TEXTAREA" });
  }
  if (tag === "INPUT") {
    const inp = /** @type {HTMLInputElement} */ (el);
    if (inp.type && inp.type !== "text" && inp.type !== "search")
      return JSON.stringify({ ok: false, reason: "unsupported_input_type", tag: inp.type });
    const start = typeof inp.selectionStart === "number" ? inp.selectionStart : inp.value.length;
    const end = typeof inp.selectionEnd === "number" ? inp.selectionEnd : inp.value.length;
    inp.value = inp.value.slice(0, start) + ins + inp.value.slice(end);
    const pos = start + ins.length;
    inp.selectionStart = inp.selectionEnd = pos;
    inp.dispatchEvent(new Event("input", { bubbles: true }));
    inp.dispatchEvent(new Event("change", { bubbles: true }));
    return JSON.stringify({ ok: true, tag: "INPUT" });
  }
  if (el.isContentEditable) {
    document.execCommand("insertText", false, ins.startsWith(sep) ? ins.slice(sep.length) : ins);
    return JSON.stringify({ ok: true, tag: "contenteditable" });
  }
  return JSON.stringify({ ok: false, reason: "unsupported_focus", tag });
})();
""".Replace("%%B64%%", b64Js, StringComparison.Ordinal);
    }
}
