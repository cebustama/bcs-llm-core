# SSoT — Editor Tooling and Agent Wizard

**Status:** Active  
**Authority:** Primary for editor orchestration and wizard behavior  
**Date:** 2026-03-17

## Scope
This document defines the implemented truth for:
- `LLMEnvSetupWindow`,
- `LLMAgentWizardWindow`,
- editor-layer policies around instructions, rebuilds, history usage, file flow, and editor-owned workflow bridging,
- and the shared estimate-precedence rule reused by adjacent editor tooling.

## 1) Canonical editor surfaces

### 1.1 `LLMEnvSetupWindow`
Purpose:
- help the developer create/update `LLMEnvSettings`,
- guide minimal `.env` creation,
- reload the env loader after changes.

Current stance:
- writes only `OPENAI_API_KEY` into `.env`,
- treats base URL and endpoints as non-secret defaults/configuration,
- exposes `allowOsEnvFallback` as a real settings control,
- expects the settings asset under `Assets/Resources/LLMEnvSettings.asset`.

### 1.2 `LLMAgentWizardWindow`
The wizard is the canonical local test harness for the package. Its current responsibilities include:
- assigning an agent asset,
- rebuilding the runtime client,
- pinging the model,
- sending prompts,
- displaying output, usage, and optional cost estimate,
- toggling history behavior,
- uploading PDFs and optionally attaching them to a request.

## 2) Rebuild semantics
The editor workflow treats the built runtime client as a snapshot of the current agent/config settings at rebuild time.

Therefore:
- editing client config values in assets does **not** retroactively mutate the live built client,
- changing base URL or endpoints requires an explicit client rebuild before new values are used.

## 3) Effective instructions
The wizard resolves effective instructions in this order:
1. wizard override text,
2. agent instructions asset text,
3. client-data `SystemInstructions`,
4. empty string.

This ordering is current implemented behavior and is authoritative for the editor tooling.

## 4) History policy

### 4.1 Core rule
The wizard distinguishes between:
- keeping local continuity in `ClientConversationHistory`,
- deciding whether to include that history in the outgoing request.

### 4.2 Current implementation
When the user disables request-history inclusion, the wizard currently:
1. snapshots the existing history,
2. clears or replaces it for the call,
3. performs the request,
4. restores the prior history,
5. optionally merges the new turn into restored history.

This is an editor policy layer over the runtime client. It does not alter the base runtime contract.

### 4.3 Invariant
The history toggle is about **request context**, not about permanently disabling local history storage.

## 5) Files panel behavior

### 5.1 Upload
The wizard supports a PDF-upload panel intended for OpenAI/editor workflows.

### 5.2 Attach to request
Current behavior is:
- attach only makes sense with OpenAI Responses,
- if the client implements `ILLMResponsesFileClient`, the wizard uses that explicit capability as the preferred path,
- if not, the wizard may use a reflection fallback against a compatible overload,
- if neither path is available, the request falls back to text-only behavior with a warning.

### 5.3 Current documentation stance
The explicit capability interface is the primary design.
Reflection is retained only as a compatibility fallback for older/custom integrations and should not be presented as the preferred integration strategy.

## 6) Usage and cost estimate display
The wizard can display token usage and an approximate cost estimate.

Current estimate precedence:
1. pricing catalog entry when available for current provider/model/tier,
2. per-client pricing fields as fallback,
3. no estimate if neither source has usable rates.

The wizard may optionally treat reasoning tokens as output tokens for estimation.


### 6.1 Adjacent tooling note — NIC Workbench
`NIC Conversation Workbench` uses the same estimate precedence as the wizard:
1. pricing catalog entry for current provider/model/tier,
2. per-client pricing fields as fallback,
3. no estimate if neither source has usable rates.

Unlike the wizard, NIC Workbench persists a per-attempt pricing snapshot into its session/diagnostic records so historical attempts do not drift when the current catalog or client fallback values change later.

This document does not become the primary authority for NIC Workbench behavior. The primary tooling home for that remains `Docs/reference/editor-tools/nic-editor-tooling.md`.

## 7) Ping behavior
Ping is a lightweight editor smoke test and should not be documented as a formal provider contract. It is a tool behavior for quick validation.

## 8) Current UX/implementation boundaries
Current editor tooling is intentionally pragmatic:
- minimal env setup,
- one canonical wizard,
- no heavy provider abstraction in UI,
- explicit rebuild requirement,
- optional pricing and file tooling,
- editor-owned manual retry UX/state when a tool needs targeted or selective retry flows.

Phase 4 / Phase 5 alignment:
- shared retry-classification surfaces are runtime-facing and optional, not editor-only configuration,
- shared orchestration surfaces now exist in runtime, but editor windows still own entrypoint selection, busy state, attempt counters, selective-retry toggles, and human-visible status,
- editor/project code may translate a shared `RetryDirective` back into local prompt-builder calls or route a narrow subflow through `LinearWorkflowRunner`,
- `PromptExecutionHelper` remains single-shot and is not replaced by editor UI code,
- Phase 5 does not require a generic workflow UI and does not move project-specific apply/replacement logic into shared core.

## 9) Active cleanup/polish items
- Add request diagnostics / payload visibility for debugging.
- Improve files panel affordances and status feedback.
- Re-evaluate whether reflection compatibility fallback is still needed once older/custom integrations are migrated.
- Align maturity/version labels (`v0`, `v0.1`, package `1.0.0`).

## 10) Not authoritative here
This document does not define:
- provider request schemas,
- env path resolution order,
- pricing catalog storage schema,
- manual regression test scripts.
Those live elsewhere.
