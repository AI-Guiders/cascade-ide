/* CIDE Monaco host bridge (ADR 0162). Offline Monaco under monaco/min/vs; posts JSON to WebView2. */
(function () {
  'use strict';

  const MONACO_VS_CANDIDATES = [
    new URL('monaco/min/vs', document.baseURI).href.replace(/\/$/, ''),
    'monaco/min/vs',
  ];

  /** @type {import('monaco-editor').editor.IStandaloneCodeEditor | null} */
  let editor = null;
  let modelVersion = 0;
  let suppressChange = false;
  let intelligenceEnabled = true;
  const decorationSets = new Map();
  const pendingRequests = new Map();
  let cfGlyphStyleEl = null;
  let disposables = [];

  function resolveMonacoVsPath() {
    for (const candidate of MONACO_VS_CANDIDATES) {
      if (candidate && !candidate.startsWith('http')) return candidate;
    }
    return MONACO_VS_CANDIDATES[0];
  }

  const MONACO_VS = resolveMonacoVsPath();

  function postToHost(msg) {
    const body = JSON.stringify(msg);
    if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
      window.chrome.webview.postMessage(body);
      return;
    }
    console.log('host←', body);
  }

  function offsetFromPosition(model, lineNumber, column) {
    return model.getOffsetAt({ lineNumber, column });
  }

  function publishModelState(reason) {
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;
    const sel = editor.getSelection();
    const caretOffset = offsetFromPosition(model, sel.positionLineNumber, sel.positionColumn);
    const selStart = offsetFromPosition(model, sel.startLineNumber, sel.startColumn);
    const selEnd = offsetFromPosition(model, sel.endLineNumber, sel.endColumn);
    postToHost({
      type: reason === 'change' ? 'editor/didChange' : 'editor/didChangeCursorSelection',
      version: modelVersion,
      text: model.getValue(),
      caretOffset,
      selectionStart: selStart,
      selectionLength: Math.max(0, selEnd - selStart),
    });
  }

  function publishScrollState() {
    if (!editor) return;
    const topLine = Math.max(1, editor.getVisibleRanges()[0]?.startLineNumber ?? 1);
    postToHost({ type: 'editor/didScroll', topLine });
  }

  function waitForHostResponse(type, requestId, timeoutMs) {
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        pendingRequests.delete(requestId);
        reject(new Error('host_timeout'));
      }, timeoutMs ?? 8000);
      pendingRequests.set(requestId, { type, resolve, reject, timer });
    });
  }

  function resolveHostResponse(msg) {
    const payload = msg.payload || msg;
    const requestId = payload.requestId;
    if (requestId == null) return;
    const pending = pendingRequests.get(requestId);
    if (!pending) return;
    clearTimeout(pending.timer);
    pendingRequests.delete(requestId);
    pending.resolve(payload);
  }

  function applyDecorations(setId, decorations) {
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;
    const monacoDecos = (decorations || []).map((d) => ({
      range: monaco.Range.fromPositions(
        model.getPositionAt(d.startOffset),
        model.getPositionAt(d.startOffset + Math.max(0, d.length))
      ),
      options: {
        className: d.className || '',
        inlineClassName: d.className || '',
        hoverMessage: d.hoverMessage ? { value: d.hoverMessage } : undefined,
        isWholeLine: false,
      },
    }));
    const handles = editor.deltaDecorations(decorationSets.get(setId) || [], monacoDecos);
    decorationSets.set(setId, handles);
  }

  function applyGutterGlyphs(glyphs) {
    if (!editor) return;
    const model = editor.getModel();
    if (!model) return;

    if (!cfGlyphStyleEl) {
      cfGlyphStyleEl = document.createElement('style');
      cfGlyphStyleEl.id = 'cide-cf-glyphs';
      document.head.appendChild(cfGlyphStyleEl);
    }

    let css = '';
    const monacoDecos = [];
    for (const g of glyphs || []) {
      const line = g.lineOneBased ?? g.line;
      if (!line) continue;
      const glyph = (g.textGlyph || '').replace(/\\/g, '\\\\').replace(/'/g, "\\'");
      const kind = g.visualKind || 'Circle';
      const cls = `cide-cf-glyph cide-cf-${kind} cide-cf-line-${line}`;
      css += `.monaco-editor .margin-view-overlays .${cls}::before { content: '${glyph}'; }\n`;
      monacoDecos.push({
        range: new monaco.Range(line, 1, line, 1),
        options: {
          glyphMarginClassName: cls,
          glyphMarginHoverMessage: g.toolTip ? { value: g.toolTip } : undefined,
        },
      });
    }
    cfGlyphStyleEl.textContent = css;
    const handles = editor.deltaDecorations(decorationSets.get('cf-gutter') || [], monacoDecos);
    decorationSets.set('cf-gutter', handles);
  }

  function registerIntelligenceProviders() {
    disposables.forEach((d) => d.dispose());
    disposables = [];

    if (!intelligenceEnabled) return;

    disposables.push(monaco.languages.registerCompletionItemProvider('csharp', {
      triggerCharacters: ['.', '@'],
      provideCompletionItems: async (model, position) => {
        const requestId = (Date.now() % 2000000000) + Math.floor(Math.random() * 1000);
        postToHost({
          type: 'editor/requestCompletion',
          requestId,
          line: position.lineNumber,
          column: position.column,
        });
        try {
          const payload = await waitForHostResponse('editor/completionResult', requestId);
          const items = (payload.items || []).map((item) => ({
            label: item.label,
            kind: monaco.languages.CompletionItemKind.Member,
            insertText: item.insertText ?? item.label,
            detail: item.detail,
          }));
          return { suggestions: items };
        } catch {
          return { suggestions: [] };
        }
      },
    }));

    disposables.push(monaco.languages.registerHoverProvider('csharp', {
      provideHover: async (model, position) => {
        const requestId = (Date.now() % 2000000000) + Math.floor(Math.random() * 1000);
        postToHost({
          type: 'editor/requestHover',
          requestId,
          line: position.lineNumber,
          column: position.column,
        });
        try {
          const payload = await waitForHostResponse('editor/hoverResult', requestId);
          if (!payload.markdown) return null;
          return { contents: [{ value: payload.markdown }] };
        } catch {
          return null;
        }
      },
    }));

    disposables.push(monaco.languages.registerSignatureHelpProvider('csharp', {
      signatureHelpTriggerCharacters: ['(', ','],
      provideSignatureHelp: async (model, position) => {
        const requestId = (Date.now() % 2000000000) + Math.floor(Math.random() * 1000);
        postToHost({
          type: 'editor/requestSignature',
          requestId,
          line: position.lineNumber,
          column: position.column,
        });
        try {
          const payload = await waitForHostResponse('editor/signatureResult', requestId);
          if (!payload.signature) return null;
          return {
            value: {
              signatures: [{ label: payload.signature, parameters: [] }],
              activeSignature: 0,
              activeParameter: 0,
            },
            dispose: () => {},
          };
        } catch {
          return null;
        }
      },
    }));
  }

  window.cideEditorHost = {
    dispatchFromHost(raw) {
      const msg = typeof raw === 'string' ? JSON.parse(raw) : raw;
      const payload = msg.payload || msg;
      switch (msg.type) {
        case 'editor/completionResult':
        case 'editor/hoverResult':
        case 'editor/signatureResult':
          resolveHostResponse(msg);
          break;
        case 'editor/setModel': {
          if (!editor) return;
          const model = editor.getModel();
          if (!model) return;
          suppressChange = true;
          try {
            modelVersion = payload.version ?? 0;
            if (payload.languageId && model.getLanguageId() !== payload.languageId) {
              monaco.editor.setModelLanguage(model, payload.languageId);
            }
            const current = model.getValue();
            if (current !== payload.text) {
              model.setValue(payload.text ?? '');
            }
          } finally {
            suppressChange = false;
          }
          publishModelState('selection');
          publishScrollState();
          break;
        }
        case 'editor/applyEdits': {
          if (!editor) return;
          const model = editor.getModel();
          if (!model) return;
          if (payload.expectedVersion != null && payload.expectedVersion !== modelVersion) {
            console.warn('applyEdits version mismatch', payload.expectedVersion, modelVersion);
            return;
          }
          suppressChange = true;
          try {
            const edits = (payload.edits || []).map((e) => ({
              range: monaco.Range.fromPositions(
                model.getPositionAt(e.startOffset),
                model.getPositionAt(e.startOffset + Math.max(0, e.length))
              ),
              text: e.text ?? '',
            }));
            if (edits.length > 0) {
              editor.executeEdits('cide-host', edits);
              modelVersion += 1;
            }
          } finally {
            suppressChange = false;
          }
          publishModelState('change');
          break;
        }
        case 'editor/setDecorations':
          applyDecorations(payload.setId || 'default', payload.decorations || []);
          break;
        case 'editor/setGutterGlyphs':
          applyGutterGlyphs(payload.glyphs || []);
          break;
        case 'editor/setStickyScroll':
          window.cideEditorHost._stickyLabel = payload.label || null;
          break;
        case 'editor/setIntelligence':
          intelligenceEnabled = payload.enabled !== false;
          registerIntelligenceProviders();
          break;
        case 'editor/setTheme':
          if (payload && payload.theme) {
            monaco.editor.setTheme(payload.theme);
          }
          break;
        default:
          break;
      }
    },
    _stickyLabel: null,
  };

  function bootEditor() {
    editor = monaco.editor.create(document.getElementById('container'), {
      value: '',
      language: 'plaintext',
      theme: 'vs-dark',
      automaticLayout: true,
      fontSize: 14,
      fontFamily: 'Consolas, Cascadia Code, monospace',
      minimap: { enabled: true },
      scrollBeyondLastLine: false,
      renderWhitespace: 'selection',
      wordBasedSuggestions: 'off',
      glyphMargin: true,
      quickSuggestions: false,
      suggestOnTriggerCharacters: true,
    });

    editor.onDidChangeModelContent(() => {
      if (suppressChange) return;
      modelVersion += 1;
      publishModelState('change');
    });

    editor.onDidChangeCursorSelection(() => publishModelState('selection'));
    editor.onDidScrollChange(() => publishScrollState());

    registerIntelligenceProviders();
    postToHost({ type: 'editor/ready', version: modelVersion });
  }

  function loadMonaco() {
    if (window.require) {
      window.require.config({ paths: { vs: MONACO_VS } });
      window.require(['vs/editor/editor.main'], bootEditor);
      return;
    }
    const loader = document.createElement('script');
    loader.src = MONACO_VS + '/loader.js';
    loader.onload = () => {
      window.require.config({ paths: { vs: MONACO_VS } });
      window.require(['vs/editor/editor.main'], bootEditor);
    };
    loader.onerror = () => postToHost({ type: 'editor/ready', error: 'monaco_load_failed' });
    document.head.appendChild(loader);
  }

  loadMonaco();
})();
