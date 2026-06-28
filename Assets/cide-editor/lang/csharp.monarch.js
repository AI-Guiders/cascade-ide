/* Patch bundled Monaco C# monarch with modern keywords (C# 9–14). */
(function (global) {
  'use strict';

  const MODERN_KEYWORDS = [
    'record', 'file', 'required', 'init', 'scoped', 'nint', 'nuint', 'and', 'or', 'not', 'when',
    'nameof', 'await', 'async', 'unsafe', 'fixed', 'sizeof', 'stackalloc', 'enum', 'interface',
    'global', 'using', 'with',
  ];

  function patchCsharpMonarch(monaco, require) {
    if (!monaco || !require) return;
    try {
      const mod = require('vs/basic-languages/csharp/csharp');
      if (!mod?.language) return;
      const lang = { ...mod.language };
      const base = Array.isArray(lang.keywords) ? lang.keywords : [];
      lang.keywords = [...new Set([...base, ...MODERN_KEYWORDS])];
      monaco.languages.setMonarchTokensProvider('csharp', lang);
      if (mod.conf) {
        monaco.languages.setLanguageConfiguration('csharp', mod.conf);
      }
    } catch (err) {
      console.warn('cide: csharp monarch patch failed', err);
    }
  }

  global.cidePatchCsharpMonarch = patchCsharpMonarch;
})(typeof window !== 'undefined' ? window : globalThis);
