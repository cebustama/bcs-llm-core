# LLM Pricing Pipeline (BCS.LLM.Core) — UPDATED 2026-01-03

This document explains the **pricing part** of the Unity ↔ LLM pipeline in the package: how token usage is collected from provider responses, how pricing rates are stored, and how approximate request cost is estimated for display in tools (e.g., your Agent/Wizard windows).

> Scope: **estimation + display**. This is **not** an invoicing system; it’s meant to be “good enough” for dashboards, editor tooling, and guardrails.

---

## 1) High-level data flow

1. Your tool calls `ILLMClient.CreateChatCompletionAsync(...)`.
2. The provider client (e.g., `OpenAILLMClient`) sends an HTTP request and receives JSON.
3. The client parses **token usage** and returns an `LLMCompletionResult`:
   - `InputTokens`
   - `CachedInputTokens` (when reported)
   - `OutputTokens`
   - `ReasoningTokens` (when reported)
   - `OutputText`
4. Your tool estimates cost by combining:
   - token usage (from `LLMCompletionResult`)
   - pricing rates (USD per 1M tokens) from either:
     - per-client config (`LLMClientData` pricing fields), or
     - a global catalog (`LLMModelPricingCatalogSO`).

---

## 2) Where token usage comes from

### 2.1 Chat Completions
`OpenAILLMClient` maps:

- Input tokens: `usage.prompt_tokens`
- Cached input: `usage.prompt_tokens_details.cached_tokens` (when present)
- Output tokens: `usage.completion_tokens`
- Reasoning: `usage.completion_tokens_details.reasoning_tokens` (when present)

### 2.2 Responses (recommended)
`OpenAILLMClient` maps:

- Input tokens: `usage.input_tokens`
- Cached input: `usage.input_tokens_details.cached_tokens`
- Output tokens: `usage.output_tokens`
- Reasoning: `usage.output_tokens_details.reasoning_tokens`

### 2.3 Notes on caching + reasoning
- Prompt caching is typically automatic; when the provider reports `cached_tokens`, those tokens are treated as discounted input tokens.
- Reasoning tokens are often billed like output tokens. The estimator supports folding them into output cost.

---

## 3) Where pricing rates are stored

### 3.1 Per-client config (`LLMClientData`)
Fields:
- `InputUSDPerMTokens`
- `CachedInputUSDPerMTokens`
- `OutputUSDPerMTokens`

Great for quick tests, but annoying for multi-model tooling.

### 3.2 Central catalog (`LLMModelPricingCatalogSO`) — recommended for tools
Each entry stores:
- `providerId`, `modelId`, `tier`
- `inputUsdPer1M`, `cachedInputUsdPer1M`, `outputUsdPer1M`
- metadata: `notes`, `source`, `lastUpdatedUtcIso`

### 3.3 Defaults helper (`OpenAIPricingCatalogExtensions`)
`ApplyOpenAIStandardTextDefaults(...)` populates the catalog with a curated list of **OpenAI model IDs + USD/1M defaults** for the STANDARD tier.

Notes:
- Some models may not list a cached-input rate; those entries set `cachedInputUsdPer1M = 0` and carry a note.
- Treat these values as **bootstrap defaults**: update periodically and/or prefer a single shared catalog in your project.

---

## 4) How cost is estimated (`LLMPricingEstimator`)

### 4.1 Token categories
- `inputTokens`
- `cachedInputTokens`
- `outputTokens`
- `reasoningTokens`

Rules:
1) cached input is clamped to input
2) reasoning can be folded into output for estimation (`treatReasoningAsOutput = true`)

### 4.2 Formula (USD per 1M tokens)
- `nonCached = input - cached`
- `nonCachedUsd = (nonCached/1_000_000) * inputUsdPer1M`
- `cachedUsd    = (cached/1_000_000) * cachedInputUsdPer1M`
- `outputUsd    = ((output [+ reasoning])/1_000_000) * outputUsdPer1M`
- `totalUsd     = nonCachedUsd + cachedUsd + outputUsd`

---

## 5) Setup in Unity (recommended)

### 5.1 Create a pricing catalog asset
**Create → LLM → Pricing → Model Pricing Catalog**

### 5.2 Populate it

#### Option A — apply defaults
Run `catalog.ApplyOpenAIStandardTextDefaults(...)` from a menu item or inspector button.

#### Option B — manual
Add only the models you actually use in v0.

---

## 6) Example usage (tool-side)
Tools (like the wizard) typically:
- read token usage from `LLMCompletionResult`
- compute `LLMPricingEstimator.TokenUsage`
- estimate with either:
  - explicit client rates, or
  - a catalog entry resolved by provider/model/tier

---

## 7) Maintenance recommendation
- Update defaults (or your catalog asset) on a schedule you can live with (e.g., monthly).
- Store `source` + `lastUpdatedUtcIso` so it’s obvious when values are stale.

---

## 8) Suggested next improvements (optional)

1. Add an “Update prices” Editor tool
   - Semi-automate the update process (open pricing page, paste numbers, validate, timestamp).
2. Add a validation utility
   - Warn if any catalog entries used by active agents have 0 rates.
3. Persist per-request logs
   - Store timestamp, model, tokens, estimated cost for later analysis.
