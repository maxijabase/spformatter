const SAMPLE = `#include <sourcemod>

public Plugin myinfo =
{
    name = "Playground",
    author = "SpFormatter",
    description = "Sample plugin",
    version = "1.0.0",
    url = ""
};

public void OnPluginStart()
{
    int x=1+2;
    if(x>0)
    {
        PrintToServer("hello");
    }
}
`;

const DEFAULT_OPTIONS = {
  indentSize: 4,
  useSpaces: true,
  spaceAfterComma: true,
  spaceAroundOperators: true,
  spaceBeforeOpenParen: false,
  spaceInArrayBrackets: false,
  newLineAfterOpenBrace: true,
  newLineAfterInclude: true,
  preserveEmptyLines: true,
  maxConsecutiveEmptyLines: 2,
  sortIncludes: false,
  requireSemicolons: true,
  allowSyntaxRecovery: false,
  allowUnsafeMacros: false,
  lineEnding: "\n"
};

let inputEditor;
let outputEditor;
let formatting = false;
let liveTimer = null;

function $(id) {
  return document.getElementById(id);
}

function readOptions() {
  return {
    indentSize: Number($("indentSize").value) || 4,
    useSpaces: $("useSpaces").checked,
    spaceAfterComma: $("spaceAfterComma").checked,
    spaceAroundOperators: $("spaceAroundOperators").checked,
    spaceBeforeOpenParen: $("spaceBeforeOpenParen").checked,
    spaceInArrayBrackets: $("spaceInArrayBrackets").checked,
    newLineAfterOpenBrace: $("newLineAfterOpenBrace").checked,
    newLineAfterInclude: $("newLineAfterInclude").checked,
    preserveEmptyLines: $("preserveEmptyLines").checked,
    maxConsecutiveEmptyLines: Number($("maxConsecutiveEmptyLines").value) || 0,
    sortIncludes: $("sortIncludes").checked,
    requireSemicolons: $("requireSemicolons").checked,
    allowSyntaxRecovery: $("allowSyntaxRecovery").checked,
    allowUnsafeMacros: $("allowUnsafeMacros").checked,
    lineEnding: "\n"
  };
}

function applyOptions(options) {
  $("indentSize").value = options.indentSize;
  $("useSpaces").checked = options.useSpaces;
  $("spaceAfterComma").checked = options.spaceAfterComma;
  $("spaceAroundOperators").checked = options.spaceAroundOperators;
  $("spaceBeforeOpenParen").checked = options.spaceBeforeOpenParen;
  $("spaceInArrayBrackets").checked = options.spaceInArrayBrackets;
  $("newLineAfterOpenBrace").checked = options.newLineAfterOpenBrace;
  $("newLineAfterInclude").checked = options.newLineAfterInclude;
  $("preserveEmptyLines").checked = options.preserveEmptyLines;
  $("maxConsecutiveEmptyLines").value = options.maxConsecutiveEmptyLines;
  $("sortIncludes").checked = options.sortIncludes;
  $("requireSemicolons").checked = options.requireSemicolons;
  $("allowSyntaxRecovery").checked = options.allowSyntaxRecovery;
  $("allowUnsafeMacros").checked = options.allowUnsafeMacros;
}

function setStatus(text) {
  $("status").textContent = text;
}

function showErrors(errors) {
  const list = $("errors");
  list.innerHTML = "";
  const markers = [];

  if (!errors || errors.length === 0) {
    list.hidden = true;
    monaco.editor.setModelMarkers(inputEditor.getModel(), "spformatter", []);
    return;
  }

  list.hidden = false;
  for (const error of errors) {
    const li = document.createElement("li");
    const where = error.startLine
      ? `L${error.startLine}:${error.startColumn} `
      : "";
    li.textContent = `${where}${error.message}`;
    list.appendChild(li);

    if (error.startLine > 0) {
      markers.push({
        severity: monaco.MarkerSeverity.Error,
        message: error.message,
        startLineNumber: error.startLine,
        startColumn: Math.max(1, error.startColumn || 1),
        endLineNumber: error.endLine || error.startLine,
        endColumn: Math.max(2, error.endColumn || (error.startColumn || 1) + 1)
      });
    }
  }

  monaco.editor.setModelMarkers(inputEditor.getModel(), "spformatter", markers);
}

async function formatNow() {
  if (formatting || !inputEditor) return;
  formatting = true;
  $("formatBtn").disabled = true;
  setStatus("Formatting…");

  const started = performance.now();
  try {
    const response = await fetch("/api/format", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        source: inputEditor.getValue(),
        options: readOptions()
      })
    });

    const elapsed = Math.round(performance.now() - started);

    if (response.status === 413 || response.status === 429 || response.status === 400) {
      const payload = await response.json().catch(() => ({}));
      showErrors([{ message: payload.error || `HTTP ${response.status}` }]);
      setStatus(`Failed · ${elapsed} ms`);
      return;
    }

    const result = await response.json();
    if (result.success) {
      outputEditor.setValue(result.text || "");
      showErrors([]);
      setStatus(`OK · ${elapsed} ms`);
    } else {
      showErrors(result.errors || [{ message: "Format failed" }]);
      setStatus(`Errors · ${elapsed} ms`);
    }
  } catch (err) {
    showErrors([{ message: err.message || String(err) }]);
    setStatus("Request failed");
  } finally {
    formatting = false;
    $("formatBtn").disabled = false;
  }
}

function scheduleLiveFormat() {
  if (!$("liveToggle").checked) return;
  clearTimeout(liveTimer);
  liveTimer = setTimeout(formatNow, 400);
}

require.config({
  paths: { vs: "https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs" }
});

async function loadVersion() {
  try {
    const health = await fetch("/api/health").then((r) => r.json());
    if (!health?.version) return;
    const short = String(health.version).split("+")[0];
    $("version").textContent = `v${short}`;
  } catch {
    /* leave blank if health is down */
  }
}

require(["vs/editor/editor.main"], () => {
  loadVersion();

  const shared = {
    language: "c",
    theme: "vs-dark",
    automaticLayout: true,
    minimap: { enabled: false },
    fontFamily: "IBM Plex Mono, Cascadia Code, Consolas, monospace",
    fontSize: 14,
    scrollBeyondLastLine: false
  };

  inputEditor = monaco.editor.create($("inputEditor"), {
    ...shared,
    value: SAMPLE
  });

  outputEditor = monaco.editor.create($("outputEditor"), {
    ...shared,
    value: "",
    readOnly: true
  });

  applyOptions(DEFAULT_OPTIONS);

  $("formatBtn").addEventListener("click", formatNow);
  $("resetOptions").addEventListener("click", () => {
    applyOptions(DEFAULT_OPTIONS);
    scheduleLiveFormat();
  });

  inputEditor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, formatNow);
  inputEditor.onDidChangeModelContent(scheduleLiveFormat);

  for (const input of document.querySelectorAll("#optionsPanel input")) {
    input.addEventListener("change", scheduleLiveFormat);
  }

  formatNow();
});
