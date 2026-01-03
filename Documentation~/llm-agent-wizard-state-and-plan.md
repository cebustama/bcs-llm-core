# LLM Agent Wizard Window — System State (v0) — UPDATED 2026-01-03

This doc tracks the current **state** of the v0 Unity Editor tooling around your provider-agnostic LLM core.

## 0) TL;DR status

- Runtime LLM core: **present**
- Pricing pipeline: **present**
- Env loader + Env setup window (writes `OPENAI_API_KEY` to `.env`): **present**
- **LLMAgentWizardWindow (EditorWindow): implemented in v0** (agent load, rebuild client, ping, prompt/response console, token usage, optional cost estimate, inline edits, history tools)

> The older “Next Work plan” is kept at the bottom as historical reference.

---

## 1) Runtime Core (what the wizard builds on)

### 1.1 Client interface + base class

- `ILLMClient`
  - shared parameters (model, temp, max output tokens, top_p, etc.)
  - system instructions
  - history store + helpers (`ClientConversationHistory`, `GetFormattedConversationHistory()`, `ClearHistory()`)
  - main async call(s): `CreateChatCompletionAsync(prompt[, instructions])`

- `LLMClientBase`
  - stores params + history list
  - provides default history utilities
  - leaves provider HTTP implementation to concrete subclasses

### 1.2 Provider implementation

- `OpenAILLMClient`
  - supports **Chat Completions** + **Responses**
  - always appends history:
    - `("user", prompt)`
    - `("assistant", result)`
  - returns `LLMCompletionResult` with usage when available

### 1.3 Factory

- `LLMClientFactory` builds a concrete `ILLMClient` from an `LLMClientData` asset (e.g., `OpenAIClientData`).

---

## 2) Env loading & setup

- `LLMEnvLoader` loads key/value pairs from (typical order):
  1) OS env var path override (if you have one)
  2) `Assets/Resources/LLMEnvSettings.asset` (if present + auto-load enabled)
  3) project root `.env`

- `LLMEnvSettings` stores *non-secret defaults* (base URL + endpoints).

- `LLMEnvSetupWindow` writes a minimal `.env` containing only:
  - `OPENAI_API_KEY=...`

---

## 3) Key behavior decision: history is always stored, sometimes used

This is implemented as a **UI-only behavior** (no runtime flag needed):

- The client **always stores** the full history.
- The EditorWindow can temporarily suppress history in the outbound request by:
  - snapshot `ClientConversationHistory`
  - clear history
  - send request (client appends the new turn)
  - merge snapshot + new turn back into the client history

This lets tools choose “Use Conversation History” without complicating the runtime API.

---

## 4) LLMAgentWizardWindow v0 features (implemented)

### 4.1 Agent selection + rebuild
- Select an `LLMAgentData` asset.
- Rebuilds the client using the agent’s `LLMClientData` (`agent.LlmClientData`) + factory.

### 4.2 Inline edits
- Edit **instructions** inline (agent instructions asset or override text).
- Edit client config values inline (model, temperature, tokens, etc.).
- Edits mark SOs dirty so values persist.

### 4.3 Test console
- **Ping**: a minimal “hello” request (so you can verify auth + connectivity).
- Prompt input + send.
- Response display + parsed usage:
  - input tokens
  - cached input tokens
  - output tokens
  - reasoning tokens (when reported)

### 4.4 Optional cost estimate
- Displays approximate cost if pricing rates are available (per-client or via catalog + estimator).

### 4.5 History tools
- Read-only history display.
- Clear history.
- History include/exclude toggle (UI-only suppression described above).

---

## 5) Known sync points to keep consistent

1. `LLMCompletionResult` must include the same token fields your OpenAI client parses:
   - `InputTokens`, `CachedInputTokens`, `OutputTokens`, `ReasoningTokens`

2. If you have both Chat Completions and Responses supported:
   - some parameters may be valid for one but rejected by the other.
   - keep the request bodies conservative for `/v1/responses` (avoid sending fields that cause `unknown_parameter`).

---

## Appendix: historical “Next Work plan” (kept for reference)

(The original incremental Step B0–B7 plan lives in the older version of this document.)
