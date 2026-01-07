# LLM Core Integration Guide (Unity EditorWindow) — v0 Pattern (UPDATED)

This document explains a **reusable pattern** to embed a small **“LLM Tools (v0)” panel** inside any existing Unity `EditorWindow`.

Use this when you want:
- A minimal, Editor-only “LLM Tools” panel (Ping + Prompt + Response preview)
- **No secrets in assets** (API keys come from `.env` / OS env vars)
- Reuse the same runtime pipeline as **LLMAgentWizardWindow** (so you don’t fork logic)
- Optional **PDF upload → file_id → attach to Responses request** (OpenAI only, PDF-only in v0)
- Optional “Use Response as JSON” (copy output into your tool’s JSON textarea)

> **Namespace note:** the package is migrating toward `BCS.LLM.Core.*`.  
> Some projects/branches still contain legacy `Eon.Narrative.LLM.*` types for backwards compatibility.  
> This guide shows the *preferred* `BCS.*` namespaces and mentions the legacy alternatives where relevant.

---

## 1) What we integrated (high-level)

Add a new foldout section to an existing `EditorWindow`:

**LLM Tools (v0)**
- Assign an `LLMAgentData` asset
- Build a runtime `ILLMClient` via `LLMClientFactory`
- **Ping** (sanity check)
- **Send Prompt** (basic request)
- Optional: history toggle (“Use Conversation History”)
- Optional: “Use Response as JSON” (copy response text into your JSON input field)
- Optional (OpenAI only): **Files (PDF Upload)** → upload PDF → get `file_id` → attach in a **Responses** request

This is intentionally the **same pattern** as the Agent Wizard:
- runtime is provider-agnostic (`ILLMClient`)
- EditorWindow orchestration controls history policy and UI state
- file upload is an optional capability (`ILLMFileClient`)

---

## 2) Prerequisites

### 2.1 LLM Core package is installed
- Add the package via UPM / git / disk reference.
- Ensure the project compiles.

### 2.2 Newtonsoft JSON dependency
Install:
- `com.unity.nuget.newtonsoft-json`

### 2.3 Local API key (no secrets in assets)
Use the env setup workflow:
- Store `OPENAI_API_KEY` in a local `.env` (gitignored), or OS env vars for CI
- Do **not** store keys in ScriptableObjects or serialized editor fields

### 2.4 Minimum agent assets
Create:
- `OpenAIClientData` (or your provider’s `LLMClientData`)
- `LLMAgentInstructionsData` (optional but recommended)
- `LLMAgentData` (links to ClientData + InstructionsData)

---

## 3) Recommended structure

### 3.1 Minimal approach (fastest)
Keep everything inside the target `EditorWindow`:
- One panel method: `DrawLlmToolsSection(...)`
- A small set of state fields
- A handful of helper methods:
  - `LlmRebuildClient()`
  - `LlmApplyClientDataToRuntimeClient()`
  - `LlmGetEffectiveInstructions()`
  - `ExecuteWithHistoryPolicyAsync(...)`
  - (optional) `LlmUploadPdfAsync()`

### 3.2 Cleaner approach (optional)
If you want more SOLID separation later:
- Extract the panel into a helper `LlmToolsPanel` class
- Keep the window responsible only for:
  - passing the target fields (e.g., `ref string jsonText`)
  - panel placement/order in UI
  - persistence of UI state

This guide focuses on the minimal approach.

---

## 4) Step-by-step implementation

### Step 1 — Add namespaces

Preferred (`BCS.*`):

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using BCS.LLM.Core.Agents;
using BCS.LLM.Core.Clients;
```

If your branch still uses legacy types, you may instead have:

```csharp
using Eon.Narrative.LLM.Agents;
using Eon.Narrative.LLM.Clients;
```

---

### Step 2 — Add minimal state fields

**Serialize** only what should persist in the Editor window (foldouts, agent selection, prompt text).  
Do **not** serialize secrets.

```csharp
// Foldout + agent
[SerializeField] private bool _showLlmTools = true;
[SerializeField] private LLMAgentData _llmAgent;

// Prompt / instructions
[SerializeField] private string _llmPrompt = "Reply with: ok";
[SerializeField] private bool _llmUseConversationHistoryInRequest = true;
[SerializeField] private bool _llmUseAgentInstructionsAsset = true;
[SerializeField] private bool _llmOverrideInstructions;
[SerializeField] private string _llmInstructionsOverride;

// Runtime-only
private ILLMClient _llmClient;
private bool _llmBusy;
private string _llmStatus = "Ready.";

// Output
private string _llmResponse;
private Vector2 _llmResponseScroll;
```

**Optional (OpenAI Files, PDF-only) state:**

```csharp
[SerializeField] private bool _showLlmFiles = false;
[SerializeField] private string _llmPdfPath;
[SerializeField] private string _llmPdfPurpose = "user_data";

[SerializeField] private bool _llmAttachLastUploadedPdfToRequest = false;

private string _llmLastUploadedFileId;
private string _llmLastUploadedFilename;
private long _llmLastUploadedBytes;
```

---

### Step 3 — Call the panel from `OnGUI()`

Place it where it makes sense in your window.  
For “generate then validate” tools, it often belongs above the JSON input.

```csharp
DrawDatabaseSection();
EditorGUILayout.Space(8);

DrawLlmToolsSection();
EditorGUILayout.Space(8);

DrawJsonSection();
```

---

### Step 4 — Implement the panel UI: `DrawLlmToolsSection()`

The panel should:
- show foldout
- allow agent assignment
- provide buttons:
  - Rebuild
  - Ping
  - Send Prompt
- show status + response preview
- optionally:
  - files foldout (PDF upload)
  - “Use Response as JSON” button (copies response into your JSON input field)

Key UI behaviors:
- When agent changes:
  - set `_llmClient = null` (forces rebuild)
  - clear response + status
- Disable buttons while `_llmBusy == true`

---

### Step 5 — Build the runtime client: `LlmRebuildClient()`

This method:
1) validates `LLMAgentData` and `agent.LlmClientData`
2) calls `LLMClientFactory.CreateClient(...)`
3) pushes config into the runtime client

```csharp
private void LlmRebuildClient()
{
    if (_llmAgent == null) { _llmStatus = "No agent selected."; return; }
    if (_llmAgent.LlmClientData == null) { _llmStatus = "Agent has no ClientData."; return; }

    _llmClient = LLMClientFactory.CreateClient(_llmAgent.LlmClientData);
    LlmApplyClientDataToRuntimeClient();

    _llmStatus = _llmClient != null ? "Client rebuilt." : "Client rebuild failed (see Console).";
}
```

---

### Step 6 — Push config into the runtime client: `LlmApplyClientDataToRuntimeClient()`

Why do this?
- It keeps behavior consistent across tools and the Agent Wizard
- It ensures the runtime client reflects the asset configuration exactly

Typical values to copy:
- model, temperature, max output tokens
- top_p, frequency penalty, stop sequences
- system instructions (baseline; overridden by `LlmGetEffectiveInstructions()` at call time)
- pricing fields (optional)

> Exact field names depend on your `LLMClientData` implementation.

---

### Step 7 — Choose “effective instructions”: `LlmGetEffectiveInstructions()`

Priority order:
1) Override textarea (`_llmOverrideInstructions`)
2) Agent instructions asset text (if `_llmUseAgentInstructionsAsset`)
3) `LLMClientData.SystemInstructions`
4) empty string

This makes it easy to test without editing assets.

---

### Step 8 — Ping: `LlmPingAsync()`

Ping should be:
- low token count
- temperature 0
- not committed to long-term history

Pattern:
- snapshot current client settings
- clamp `MaxOutputTokens` + `Temperature`
- send request with fixed prompt + instructions
- restore settings in `finally`

---

### Step 9 — Send Prompt: `LlmSendPromptAsync()`

The standard request path:
- gather `prompt`
- gather `instructions` (`LlmGetEffectiveInstructions()`)
- optionally attach file_ids (OpenAI Responses)
- apply history policy (include or suppress)

---

## 5) The key reusable trick: history policy without changing the runtime API

### Problem
The runtime client stores history automatically, but tools sometimes want to:
- send a request *without* history (clean context)
- while keeping local history continuity for future calls

### Solution
Snapshot + swap + call + restore:

1) clone current history
2) set history to empty list
3) call the LLM (client appends the new turn to the empty list)
4) clone that “new turn”
5) restore original snapshot
6) optionally merge the new turn into restored history

This remains an **Editor UI policy** (no runtime flag required).

### Optional file support
If you attach a PDF, you typically still want the same history behavior.  
So your “execute” helper can accept an optional `IReadOnlyList<string> fileIds` and:
- call a file-capable method when available
- otherwise fall back to text-only

**Recommended approach (no new interface required):**
- keep `ILLMClient` unchanged
- in Editor code, attempt to call a known overload via:
  - safe cast to OpenAI client type, **or**
  - reflection (if you want to avoid referencing provider types directly)

---

## 6) Files (PDF upload) — how to wire it in

### 6.1 Upload a PDF → `file_id` (OpenAI only)
File upload is exposed as an optional capability:

- `ILLMFileClient.UploadFileAsync(filePath, purpose)`

Editor-side pattern:

```csharp
private async Task LlmUploadPdfAsync()
{
    if (_llmClient == null) { _llmStatus = "No client. Rebuild first."; return; }
    if (!(_llmClient is ILLMFileClient fileClient))
    {
        _llmStatus = "Client does not support file upload (ILLMFileClient missing).";
        return;
    }

    if (string.IsNullOrWhiteSpace(_llmPdfPath))
    {
        _llmStatus = "No PDF path selected.";
        return;
    }

    var result = await fileClient.UploadFileAsync(_llmPdfPath, _llmPdfPurpose);

    _llmLastUploadedFileId = result.FileId;
    _llmLastUploadedFilename = result.Filename;
    _llmLastUploadedBytes = result.Bytes;

    _llmStatus = $"Uploaded: {_llmLastUploadedFileId}";
}
```

### 6.2 Attach a PDF to a request (Responses only)
Attaching files is a **Responses API** feature.  
So in your client data, ensure:
- **API Variant = Responses**

Then, in your Send Prompt path, build `fileIds`:

```csharp
IReadOnlyList<string> fileIds = null;
if (_llmAttachLastUploadedPdfToRequest && !string.IsNullOrWhiteSpace(_llmLastUploadedFileId))
    fileIds = new[] { _llmLastUploadedFileId };
```

Pass `fileIds` into your execute helper. If your underlying OpenAI client supports “responses-with-files”, it will use them; otherwise it will fall back to text-only and log a warning.

---

## 7) “Use Response as JSON” pattern

If your tool has a JSON textarea (e.g., importer input), add a button:

**Use Response as JSON**
- copies `_llmResponse.Trim()` into your JSON field
- resets any derived UI state (parse cache, dry-run cache, validation report)

This prevents stale “plan/issues” panels from referencing old JSON.

---

## 8) Common pitfalls & troubleshooting

### 8.1 Assembly definition references
If your project uses `.asmdef` files:
- The **editor assembly** containing your `EditorWindow` must reference the assemblies that define:
  - `LLMAgentData`
  - `ILLMClient`
  - `LLMClientFactory`
  - `ILLMFileClient` (if using files)
  - any OpenAI provider types you reference directly

### 8.2 “unknown_parameter” from `/v1/responses`
This usually means the provider request payload is out of date for the Responses API.
- Update your OpenAI client implementation to match the current Responses schema.
- Keep the `/v1/responses` request body conservative and avoid sending invalid fields.

### 8.3 File attachments don’t work
Check:
- your OpenAI client config uses **Responses**
- you actually have a valid `file_id`
- your send path is toggling “Attach last uploaded PDF”
- you are not accidentally calling the Chat Completions endpoint

### 8.4 Busy state stuck true
Make sure you:
- wrap requests in try/catch/finally
- reset `_llmBusy` in a safe UI continuation (`EditorApplication.delayCall`)
- call `Repaint()` after state changes

### 8.5 Do not serialize secrets
Never store API keys in:
- ScriptableObjects
- `[SerializeField] string` in editor windows
- project settings assets

Use `.env` / OS env vars and ignore `.env` in git.

---

## 9) Reusable checklist (copy/paste into PR description)

### Minimal LLM Tools panel
- [ ] Add namespaces (BCS.* or legacy Eon.*)
- [ ] Add serialized UI state (foldout, agent, prompt)
- [ ] Add runtime fields (client, busy, status, response)
- [ ] Add `DrawLlmToolsSection()` call in `OnGUI()`
- [ ] Implement:
  - [ ] `LlmRebuildClient()`
  - [ ] `LlmApplyClientDataToRuntimeClient()`
  - [ ] `LlmGetEffectiveInstructions()`
  - [ ] `ExecuteWithHistoryPolicyAsync(...)`
  - [ ] `LlmPingAsync()`
  - [ ] `LlmSendPromptAsync()`
- [ ] Ensure keys are loaded from `.env` / env loader (not assets)

### Optional PDF workflow (OpenAI only)
- [ ] Add PDF state fields (path, purpose, lastUpload)
- [ ] Add “Files (PDF Upload)” foldout UI
- [ ] Upload PDF using `ILLMFileClient.UploadFileAsync`
- [ ] Attach `file_id` in a Responses request (toggle)
- [ ] Fallback to text-only when files aren’t supported

---

## 10) Extension points (next sensible steps)

Once the basic panel works:
- Add “copy response to JSON + auto-validate” workflow for your importer
- Add “single vs batch extraction” UI for multi-species PDFs
- Add richer request telemetry (duration, bytes, usage deltas)
- Extract the panel into `LlmToolsPanel` if multiple windows need it

---

## Appendix — Suggested doc placement

Store this guide under one of:
- `Docs/LLM/LLM_Core_EditorWindow_Integration_Guide.md`
- `Documentation~/LLM_Core_EditorWindow_Integration_Guide.md` (Unity package convention)
