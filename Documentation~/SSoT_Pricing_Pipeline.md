# SSoT — Pricing Pipeline

**Status:** Active  
**Authority:** Primary for pricing-catalog and cost-estimation semantics  
**Date:** 2026-03-29

## Scope
This document defines the implemented truth for:
- pricing catalog storage,
- pricing lookup keys and tiers,
- estimation math,
- default seeding for OpenAI pricing,
- editor-side application of default pricing data,
- and the shared precedence used by tooling that persists per-attempt pricing snapshots.

## 1) Purpose
The pricing pipeline exists to provide approximate request-cost estimates for tooling. It is:
- non-secret,
- local and editor-friendly,
- independent of provider billing APIs,
- suitable for estimates, not invoices.

## 2) Authoritative catalog asset
`LLMModelPricingCatalogSO` is the authoritative storage surface for central pricing data.

### 2.1 What an entry contains
Each price entry stores:
- `providerId`
- `modelId`
- `tier`
- `inputUsdPer1M`
- `cachedInputUsdPer1M`
- `outputUsdPer1M`
- optional notes

### 2.2 Metadata
The catalog also stores:
- `source`
- `lastUpdatedUtcIso`

These are informational metadata fields and should be maintained when defaults are refreshed.

### 2.3 Lookup key
Lookup is currently based on:
`providerId :: modelId :: tier`

The in-memory cache is case-insensitive and trim-normalized. If duplicates exist, the last entry wins.

## 3) Estimation math
`LLMPricingEstimator` is a pure utility layer:
- no API calls,
- no editor dependency in the core math,
- accepts token usage and per-1M-token rates.

### 3.1 Usage model
The estimator accepts:
- input tokens,
- cached input tokens,
- output tokens,
- reasoning tokens.

### 3.2 Guards and assumptions
- negative values are clamped to zero,
- cached input tokens cannot exceed input tokens,
- reasoning tokens can be optionally treated as output tokens.

### 3.3 Output
The estimator returns:
- non-cached input USD,
- cached input USD,
- output USD,
- total USD.

## 4) Price-source precedence in tooling
Current wizard precedence is:
1. central catalog entry for current provider/model/tier,
2. per-client pricing fields copied into the runtime client,
3. no estimate if no usable pricing exists.

The catalog is the preferred authoritative source for tooling.


### 4.1 Snapshotting editor tooling
Tooling that persists a per-attempt pricing snapshot (for example the NIC Conversation Workbench) still uses the same precedence above. The difference is storage, not semantics:
- the estimate is resolved once during the attempt,
- the resulting provider/model/source/rates/estimated-USD snapshot is stored with the attempt record,
- and later diagnostics should read that stored snapshot rather than recomputing from the current catalog or current client state.

This keeps historical attempt diagnostics stable even when catalog entries or fallback client pricing values change later.

## 5) OpenAI default seeding
`OpenAIPricingCatalogExtensions.ApplyOpenAIStandardTextDefaults(...)` exists to bootstrap a catalog with a curated set of OpenAI standard-tier text-token defaults.

Current characteristics:
- fills the `Standard` tier,
- updates metadata when requested,
- upserts by provider/model/tier,
- can overwrite existing entries or fill only missing rates,
- uses `0` for cached-input rates when the source table does not list a cached-input price,
- should cover the model IDs that are actually exposed by `OpenAIClientData` in the current package build.

Current practical guidance:
- refresh catalog entries whenever the exposed OpenAI model list changes,
- keep the seeded catalog aligned with the exact runtime `modelId` strings used by `OpenAIClientData`,
- do not assume `gpt-5-mini` and `gpt-5.4-mini` are interchangeable IDs,
- do not assume `gpt-5-nano` and `gpt-5.4-nano` are interchangeable IDs.

Current high-priority text-model coverage should include whichever of these IDs your package exposes:
- `gpt-5.4`
- `gpt-5.4-mini`
- `gpt-5.4-nano`
- `gpt-5.4-pro`
- `gpt-5.2`
- `gpt-5.2-pro`
- `gpt-5-mini`
- `gpt-5-nano`

Specialized or compatibility IDs may also be seeded when the package deliberately exposes them, but the core requirement is parity between the catalog and the actual selector.

### 5.1 Current OpenAI-specific caveats
The pricing catalog is still an approximate tooling surface. It does not automatically model every provider-side pricing nuance unless those rules are explicitly represented in the catalog/tooling policy.

Current caveats worth documenting for OpenAI text models:
- `gpt-5.4` and `gpt-5.4-pro` have a long-context pricing rule for prompts above 272K input tokens,
- regional processing (data residency) endpoints add a 10% uplift for `gpt-5.4`, `gpt-5.4-pro`, `gpt-5.4-mini`, and `gpt-5.4-nano`,
- when an official model page does not list a cached-input price, the current bootstrap policy remains `cachedInputUsdPer1M = 0` plus an explanatory note.

### 5.2 Practical refresh / usage loop
Recommended workflow:
1. Create or open an `LLMModelPricingCatalogSO`.
2. Apply OpenAI defaults through `ApplyOpenAIStandardTextDefaults(...)` or the editor menu action.
3. Verify that every model currently exposed in `OpenAIClientData` has a matching `providerId :: modelId :: tier` entry in the catalog.
4. Assign that catalog to the wizard/tooling path that estimates cost.
5. Treat per-client pricing fields only as a fallback path, not the preferred source of truth.
6. Update `source` and `lastUpdatedUtcIso` whenever rates are refreshed.


## 6) Editor menu action
`OpenAIPricingCatalogMenu` provides an editor action to apply OpenAI standard defaults to the currently selected catalog asset.

This is convenience tooling, not a replacement for catalog review.

## 7) Current maintenance policy
When pricing defaults are refreshed:
- update catalog metadata,
- note the change in `changelog-ssot.md` if semantics or authority changed,
- do not present the resulting estimate as billing reconciliation.

## 8) Known boundaries
- This system does not fetch live provider billing.
- This system does not guarantee that the provider's invoice format matches the estimate presentation.
- This system only knows what rates are entered in the catalog or copied into client data.
- Model aliases and deprecated IDs may still exist for compatibility; catalog coverage should match the models actually used by the package.

## 9) Not authoritative here
This document does not define:
- provider request generation,
- env/secrets handling,
- wizard history semantics.
