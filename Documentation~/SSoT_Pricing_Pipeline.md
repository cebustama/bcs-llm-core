# SSoT — Pricing Pipeline

**Status:** Active  
**Authority:** Primary for pricing-catalog and cost-estimation semantics  
**Date:** 2026-03-16

## Scope
This document defines the implemented truth for:
- pricing catalog storage,
- pricing lookup keys and tiers,
- estimation math,
- default seeding for OpenAI pricing,
- editor-side application of default pricing data.

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

## 5) OpenAI default seeding
`OpenAIPricingCatalogExtensions.ApplyOpenAIStandardTextDefaults(...)` exists to bootstrap a catalog with a curated set of OpenAI standard-tier text-token defaults.

Current characteristics:
- fills the `Standard` tier,
- updates metadata when requested,
- upserts by provider/model/tier,
- can overwrite existing entries or fill only missing rates,
- uses `0` for cached-input rates when the source table does not list a cached-input price.

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
