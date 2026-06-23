/* Monarch grammar for TOML (ADR 0163 M9). */
(function (global) {
  'use strict';

  function registerTomlMonarch(monaco) {
    if (!monaco || !monaco.languages) return;
    monaco.languages.register({ id: 'toml' });
    monaco.languages.setMonarchTokensProvider('toml', {
      defaultToken: '',
      tokenPostfix: '.toml',
      brackets: [
        { open: '{', close: '}', token: 'delimiter.bracket' },
        { open: '[', close: ']', token: 'delimiter.square' },
      ],
      keywords: ['true', 'false'],
      tokenizer: {
        root: [
          { include: '@whitespace' },
          [/^\s*\[\[\s*[^\]]+\s*\]\]/, 'type.identifier'],
          [/^\s*\[\s*[^\]]+\s*\]/, 'type.identifier'],
          [/([A-Za-z0-9_-]+)(\s*)(=)/, ['key', 'white', 'delimiter']],
          [/#.*$/, 'comment'],
          [/"([^"\\]|\\.)*$/, 'string.invalid'],
          [/"/, { token: 'string.quote', next: '@stringDouble' }],
          [/'([^'\\]|\\.)*$/, 'string.invalid'],
          [/'/, { token: 'string.quote', next: '@stringSingle' }],
          [/\b(?:true|false)\b/, 'keyword'],
          [/-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?/, 'number'],
          [/[,]/, 'delimiter'],
        ],
        whitespace: [[/\s+/, 'white']],
        stringDouble: [
          [/[^\\"]+/, 'string'],
          [/\\./, 'string.escape'],
          [/"/, { token: 'string.quote', next: '@pop' }],
        ],
        stringSingle: [
          [/[^\\']+/, 'string'],
          [/\\./, 'string.escape'],
          [/'/, { token: 'string.quote', next: '@pop' }],
        ],
      },
    });
  }

  global.cideRegisterMonarchGrammars = registerTomlMonarch;
})(typeof window !== 'undefined' ? window : globalThis);
