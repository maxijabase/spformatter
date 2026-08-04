const FORMAT_SAMPLE = `#include <sourcemod>

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

// Exhaustive legacy sample: hits every default SpModernizer rule family.
const MODERNIZE_SAMPLE = `new Float:g_x = 5.0;
new g_y = 7;
new String:g_name[32];
new bool:g_ready;
decl String:g_scratch[64];
new Handle:g_timer;
static Float:g_static = 1.0;

stock bool:IsUserACrab(client)
{
    return false;
}

public OnReceivedString(const String:name[], Float:fval)
{
    new Float:scaled = Float:fval;
    new _:plain = 0;
    PrintToServer("%s %f", name, scaled);
}

forward Action:OnSomething(Handle:timer, any:data);

native Float:NativeAdd(Float:a, Float:b);

functag public Action:SrvCmd(args);
functag public ConCmd(client, args);

funcenum Timer {
    Action:public(Handle:timer, Handle:hndl),
    Action:public(Handle:timer),
};

struct PluginInfo {
    const String:name[],
    const String:author[],
    const String:description[],
    const String:version[],
    const String:url[]
};

public OnPluginStart()
{
    new Float:local = Float:0;
    new String:buf[32];
    new i = 0;
    new Handle:arr;

    for (new j = 0; j < 3; j++)
    {
        local = Float:j;
    }

    while !g_ready do
    {
        g_ready = true;
    }

    do
    {
        i++;
    }
    while !g_ready;
}

public void AlreadyModern(int client, const char[] msg)
{
    float ok = 1.0;
    ArrayList list = new ArrayList();
    int[] players = new int[MaxClients + 1];
    delete list;
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
  lineEnding: "lf"
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
    lineEnding: $("lineEnding").value === "crlf" ? "crlf" : "lf"
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
  $("lineEnding").value = options.lineEnding === "crlf" ? "crlf" : "lf";
}

function setStatus(text) {
  const el = $("status");
  el.textContent = text;
  el.classList.remove("is-ok", "is-error");
  if (String(text).startsWith("OK")) el.classList.add("is-ok");
  else if (/error|fail/i.test(String(text))) el.classList.add("is-error");
}

function getMode() {
  const pressed = document.querySelector(".mode-seg__btn[aria-pressed='true']");
  return pressed?.dataset.mode === "modernize" ? "modernize" : "format";
}

function setMode(mode, options = {}) {
  const next = mode === "modernize" ? "modernize" : "format";
  for (const btn of document.querySelectorAll(".mode-seg__btn")) {
    btn.setAttribute("aria-pressed", btn.dataset.mode === next ? "true" : "false");
  }
  syncModeUi(options);
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

  const mode = getMode();
  const isModernize = mode === "modernize";
  setStatus(isModernize ? "Modernizing…" : "Formatting…");

  const started = performance.now();
  try {
    const endpoint = isModernize ? "/api/modernize" : "/api/format";
    const body = isModernize
      ? {
          source: inputEditor.getValue(),
          formatAfter: $("formatAfter").checked,
          options: readOptions()
        }
      : {
          source: inputEditor.getValue(),
          options: readOptions()
        };

    const response = await fetch(endpoint, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
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
      const extra = isModernize && result.changes
        ? ` · ${result.changes.length} change(s)`
        : "";
      setStatus(`OK · ${elapsed} ms${extra}`);
    } else {
      showErrors(result.errors || [{ message: isModernize ? "Modernize failed" : "Format failed" }]);
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

function sampleForMode(mode) {
  return mode === "modernize" ? MODERNIZE_SAMPLE : FORMAT_SAMPLE;
}

function isStockSample(text) {
  const normalized = String(text || "").replace(/\r\n/g, "\n");
  return (
    normalized === FORMAT_SAMPLE.replace(/\r\n/g, "\n") ||
    normalized === MODERNIZE_SAMPLE.replace(/\r\n/g, "\n") ||
    normalized.trim() === ""
  );
}

function syncModeUi(options = {}) {
  const modernize = getMode() === "modernize";
  $("formatAfterWrap").hidden = !modernize;

  const inputTitle = $("inputPaneTitle");
  const outputTitle = $("outputPaneTitle");
  if (inputTitle) inputTitle.textContent = modernize ? "Input · Modernize" : "Input · Format";
  if (outputTitle) outputTitle.textContent = modernize ? "Modernized" : "Formatted";

  if (options.loadSample && inputEditor && isStockSample(inputEditor.getValue())) {
    inputEditor.setValue(sampleForMode(modernize ? "modernize" : "format"));
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

  const params = new URLSearchParams(window.location.search);
  const initialMode = params.get("mode") === "modernize" ? "modernize" : "format";

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
    value: sampleForMode(initialMode)
  });

  outputEditor = monaco.editor.create($("outputEditor"), {
    ...shared,
    value: "",
    readOnly: true
  });

  applyOptions(DEFAULT_OPTIONS);
  setMode(initialMode);

  $("formatBtn").addEventListener("click", formatNow);
  $("modeSeg").addEventListener("click", (event) => {
    const btn = event.target.closest(".mode-seg__btn");
    if (!btn || btn.getAttribute("aria-pressed") === "true") return;
    setMode(btn.dataset.mode, { loadSample: true });
    formatNow();
  });
  $("formatAfter").addEventListener("change", scheduleLiveFormat);
  $("resetOptions").addEventListener("click", () => {
    applyOptions(DEFAULT_OPTIONS);
    if (inputEditor) {
      inputEditor.setValue(sampleForMode(getMode()));
    }
    formatNow();
  });

  inputEditor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, formatNow);
  inputEditor.onDidChangeModelContent(scheduleLiveFormat);

  for (const input of document.querySelectorAll("#optionsPanel input")) {
    input.addEventListener("change", scheduleLiveFormat);
  }

  formatNow();
});
