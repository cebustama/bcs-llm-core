# LLM Agent Wizard Window — System State & Plan (v0.1) — UPDATED 2026-01-07

This document describes the **current state** of the LLM Core + the **LLMAgentWizardWindow** Editor tooling, and proposes a small set of next improvements.

> Scope: Unity **Editor-only** tooling and the reusable provider-agnostic runtime pipeline (LLM Core).  
> Provider focus: **OpenAI** (Chat Completions + Responses + Files upload + Responses file attachments).

---

## 0) TL;DR status

### Runtime LLM Core
- ✅ `ILLMClient` abstraction (provider-agnostic)
- ✅ OpenAI provider client (`OpenAILLMClient`)
- ✅ Supports **Chat Completions** and **Responses** variants (selectable in `OpenAIClientData`)
- ✅ Token usage parsing into `LLMCompletionResult`
- ✅ Pricing pipeline (optional estimate)

### Environment / Secrets
- ✅ `LLMEnvLoader` reads `.env` / OS env vars
- ✅ `LLMEnvSetupWindow` writes `OPENAI_API_KEY` to local `.env`
- ✅ No keys stored in ScriptableObjects / assets

### Files (PDF) support
- ✅ `ILLMFileClient.UploadFileAsync(...)` (OpenAI Files API)
- ✅ Editor wizard supports: **PDF path → Upload → file_id**
- ✅ OpenAI Responses requests can **attach file_id** (PDF-only in v0.1)
- ✅ Important schema fix: Responses(file parts) JSON serialization must **ignore nulls** to avoid `unknown_parameter` errors.

### Editor Tooling
- ✅ `LLMAgentWizardWindow` is the canonical test harness:
  - Agent selection + client rebuild
  - Ping
  - Prompt console
  - History policy toggle (“Use Conversation History in request”)
  - Usage display + optional cost estimate
  - Files (PDF upload) + attach toggle (Responses only)

---

## 1) Core architecture (what the wizard builds on)

### 1.1 Data assets
- `LLMAgentData`
  - References an `LLMClientData` (e.g. `OpenAIClientData`)
  - Optionally references `LLMAgentInstructionsData` (instructions text)
- `OpenAIClientData`
  - Model, temperature, max output tokens, etc.
  - `ApiVariant` toggle: Chat Completions vs Responses
  - Endpoints: Chat / Responses / Files
  - Pricing fields (optional)

### 1.2 Runtime interfaces
- `ILLMClient`
  - `CreateChatCompletionAsync(prompt)` / `CreateChatCompletionAsync(prompt, instructions)`
  - Manages local in-memory `ClientConversationHistory`
- `ILLMFileClient` (optional capability)
  - `UploadFileAsync(filePath, purpose)` → returns `file_id`
- (Optional) `ILLMResponsesFileClient`
  - May exist, but current wizard flow can work without it (reflection/overload based).

### 1.3 Provider client: OpenAI
`OpenAILLMClient`:
- Uses `HttpClient` with `Authorization: Bearer <OPENAI_API_KEY>`
- Implements:
  - Chat Completions requests (text-only)
  - Responses requests (text-only)
  - Responses requests with **file parts** (`input_file` + `input_text`) **when fileIds are provided**
  - Files API upload: `POST /v1/files` (PDF-only enforced by client)
- Important behavior:
  - If **no fileIds** are passed → request is **plain text** (no sticky file mode)
  - Attaching files is **request-scoped**; file_ids are not stored in history

---

## 2) Wizard behavior (what it guarantees)

### 2.1 History policy (crucial invariant)
- The runtime client always stores turns locally (`ClientConversationHistory`).
- The wizard allows a **request policy**:
  - **Use Conversation History (in request) ON**: history is included in the API request.
  - **OFF**: history is temporarily replaced with an empty list for the call, then restored, and the new turn is merged back.

This lets you test “clean context” without losing continuity.

### 2.2 Instructions precedence (recommended)
1. Instructions override textarea (if enabled)
2. Agent instructions asset (`LLMAgentInstructionsData`) (if enabled)
3. ClientData system instructions
4. Empty string

### 2.3 Files attachment behavior (OpenAI Responses)
- Upload step produces `file_id`.
- Attach step is gated by:
  - ApiVariant == **Responses**
  - Attach toggle ON
  - A non-empty `file_id`
- If any condition fails, the wizard sends text-only.

---

## 3) “Why Responses?” and API compatibility notes
- The v0.1 “PDF file_id input” workflow is implemented using **OpenAI Responses** input parts:
  - `{ type: "input_file", file_id: "..." }`
  - `{ type: "input_text", text: "..." }`
- The same request body must **not** include invalid fields.
- Json.NET default behavior can serialize nulls; for file-part payloads we must ignore null fields to prevent errors like:
  - `Unknown parameter: input[0].content[0].text`

---

## 4) Known limitations / choices

### 4.1 PDF-only in v0.1
- Upload enforces `.pdf`.
- No DOCX support.

### 4.2 Endpoint edits require rebuild
- `HttpClient.BaseAddress` and endpoint strings are set in `OpenAILLMClient` constructor.
- If you edit base URL or endpoint fields on the ClientData, you must **Rebuild Client** to apply them.

### 4.3 Interface vs reflection for “files in request”
Current state:
- The wizard can call an overload (if present) using reflection, so it doesn’t require a new runtime interface.

Recommended future:
- Prefer a capability interface (e.g. `ILLMResponsesFileClient`) to avoid reflection and to support other providers cleanly.

---

## 5) Next work plan (small, high-leverage)

### 5.1 Make file-attach capability explicit (remove reflection)
- Implement `ILLMResponsesFileClient` on OpenAI client
- Wizard calls interface first; reflection only as fallback (or remove reflection entirely)

### 5.2 Add request diagnostics (debug-only)
- “Log request JSON” toggle (Editor only)
- Store last request payload in memory for copy/paste debugging

### 5.3 UX improvements in Files panel
- “Attach last uploaded file” default ON (optional)
- Show active ApiVariant and disable/tooltip controls accordingly
- Add “Clear last file_id” button

### 5.4 Document + versioning
- Add `CHANGELOG.md` entry:
  - “v0.1: Added Files API upload + Responses file attachment (PDF-only)”

---

## 6) Regression watchlist (things that must not break)
- Text-only paths must remain unchanged when no fileIds are supplied
- History policy must always store turns, regardless of includeHistoryInRequest
- CachedInputTokens must clamp ≤ InputTokens (defensive)
- Busy flag must always reset on exceptions
