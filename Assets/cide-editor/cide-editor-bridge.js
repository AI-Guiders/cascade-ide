/* CIDE Monaco host bridge (ADR 0162). Loads Monaco from jsDelivr; posts JSON to WebView2. */
(function () {
  'use strict';

  const MONACO_VS = 'https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs';

  /** @type {import('monaco-editor').editor.IStandaloneCodeEditor | null} */
  let editor = null;
  let modelVersion = 0;
  let suppressChange = false;
  const decorationSets = new Map();

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

  window.cideEditorHost = {
    dispatchFromHost(raw) {
      const msg = typeof raw === 'string' ? JSON.parse(raw) : raw;
      const payload = msg.payload || msg;
      switch (msg.type) {
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
        case 'editor/setTheme':
          if (payload && payload.theme) {
            monaco.editor.setTheme(payload.theme);
          }
          break;
        default:
          break;
      }
    },
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
    });

    editor.onDidChangeModelContent(() => {
      if (suppressChange) return;
      modelVersion += 1;
      publishModelState('change');
    });

    editor.onDidChangeCursorSelection(() => publishModelState('selection'));

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
