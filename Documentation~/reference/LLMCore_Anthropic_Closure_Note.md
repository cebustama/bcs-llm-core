# LLM Core — Anthropic Provider Closure Note

**Date:** 2026-05-28
**Package:** BCS / Eon LLM Core (separate from MidiGenPlay)
**Trigger:** MidiGenPlay L1 needed a provider after OpenAI access lapsed.

This is a closure note for LLM Core's own governed docs. LLM Core has its own
SSoT set (`SSoT_Runtime_and_OpenAI_Provider.md`, `SSoT_Editor_Tooling_and_Wizard.md`,
`CURRENT_STATE.md`, `changelog-ssot.md`, `Roadmap_LLM_Core.md`). Apply these where
they fit your LLM Core governance.

---

## What changed in LLM Core

### Added
- `Runtime/Clients/Anthropic/AnthropicClientData.cs` — concrete `LLMClientData`
  for Anthropic. 4-model enum (Opus 4.7, Opus 4.6, Sonnet 4.6, Haiku 4.5),
  reads `ANTHROPIC_API_KEY` from env, `BaseUrl`/`MessagesEndpoint` from
  `LLMEnvSettings`. `ApiVersion` const pinned to `2023-06-01`.
- `Runtime/Clients/Anthropic/AnthropicLLMClient.cs` — concrete `ILLMClient`
  (extends `LLMClientBase`). `POST /v1/messages`, headers `x-api-key` +
  `anthropic-version`. System prompt as top-level field; messages array of
  user/assistant only (system/developer roles filtered from history). Parses
  `content[0].text` → `OutputText`, `usage.input_tokens`/`output_tokens`.
  Private request/response DTOs inline.

### Modified
- `Runtime/Clients/LLMClientData.cs` — `LLMProvider` enum gains `Anthropic`
  (appended at end for serialization safety; existing `OpenAI=0` assets
  unaffected).
- `Runtime/Clients/LLMClientFactory.cs` — `case LLMProvider.Anthropic` →
  `new AnthropicLLMClient(clientData as AnthropicClientData)`.
- `Runtime/Env/LLMEnvSettings.cs` — two new fields: `anthropicBaseUrl`
  (default `https://api.anthropic.com`), `anthropicMessagesEndpoint`
  (default `/v1/messages`).
- `Editor/LLMEnvSetupWindow.cs` — generalized to multi-provider:
  - Status panel reports both `OPENAI_API_KEY` and `ANTHROPIC_API_KEY`.
  - Parallel OpenAI / Anthropic sections (D-AC4 = b).
  - **`.env` write is now read-merge-write** (`WriteOrUpdateEnvKey`) — preserves
    existing keys and comments instead of overwriting. (Prior behavior wiped the
    file to a single line; that was a latent bug.)
  - Per-provider **Ping** button — builds a temp in-memory client (max_tokens 16),
    sends "ping", reports OK/latency/tokens or a clear error via dialog.

### Behavior notes / gotchas resolved
- **`temperature` + `top_p` mutual exclusivity.** Newer Anthropic models reject
  a request that specifies both ("`temperature` and `top_p` cannot both be
  specified"). `AnthropicLLMClient` sends `temperature` only and omits `top_p`
  (nullable DTO field + `NullValueHandling.Ignore`). `top_p = 1.0` is a no-op
  anyway.
- **Async/editor pattern.** Pings use `async void` + `await`; never
  `.GetAwaiter().GetResult()` on the main thread (deadlocks the editor).

---

## Suggested governed-doc edits (LLM Core)

1. **Rename / re-scope `SSoT_Runtime_and_OpenAI_Provider.md`.** The title is now
   misleading — there are two providers. Options:
   - Rename to `SSoT_Runtime_and_Providers.md` and add an Anthropic section, OR
   - Keep it and add a sibling `SSoT_Anthropic_Provider.md`.
   Recommendation: rename to `…_and_Providers.md` — one provider-runtime SSoT
   with per-provider subsections scales better than N sibling files.

2. **`SSoT_Editor_Tooling_and_Wizard.md` §1.1** (`LLMEnvSetupWindow`) — update
   the "writes only `OPENAI_API_KEY`" stance to reflect multi-provider key
   writing and the read-merge-write behavior. Add the per-provider Ping button
   to the wizard's responsibilities list.

3. **`CURRENT_STATE.md`** — add Anthropic provider to "current true state"
   (provider-agnostic core now has OpenAI **and** Anthropic implementations).

4. **`changelog-ssot.md`** — add a 2026-05-28 "Anthropic provider added" entry
   using the Added/Modified/Behavior shape above.

5. **`Roadmap_LLM_Core.md`** — record the Anthropic provider as a closed batch.

---

## Flagged design item (not blocking)

`LLMClientData` exposes `Temperature` and `TopP` as independent fields, but
Anthropic treats them as mutually exclusive. The current resolution (always
prefer temperature, omit top_p) is the right default. If top_p sampling on
Anthropic is ever wanted, add an explicit `SamplingMode { Temperature, TopP }`
control on `AnthropicClientData` rather than inferring intent from field values.
Surface this in the provider SSoT as a known constraint.
