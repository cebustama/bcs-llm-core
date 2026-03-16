# SSoT Index — BCS / Eon LLM Core

**Status:** Active  
**Authority:** Primary map of documentation ownership  
**Date:** 2026-03-16

## Scope
This index defines where authoritative truth lives for the package.

## Core governance docs
- `SSoT_CONTRACTS.md`
- `coverage-matrix.md`
- `changelog-ssot.md`
- `CURRENT_STATE.md`
- `planning/Roadmap_LLM_Core.md`

## Authoritative subsystem docs
- `SSoT_Runtime_and_OpenAI_Provider.md`
- `SSoT_Editor_Tooling_and_Wizard.md`
- `SSoT_Pricing_Pipeline.md`

## Support docs
- `reference/LLM_Core_EditorWindow_Integration_Guide.md`
- `reference/llm-agent-wizard-test-cases.md`

## Archived docs
- `archive/llm-agent-wizard-state-and-plan.md`
- `archive/llm-pricing-pipeline.md`

## Global invariants
- Secrets do not belong in assets.
- `LLMClientData` is the provider-agnostic client config base; provider-specific behavior is specialized elsewhere.
- The runtime contract is `ILLMClient`; file upload and Responses-with-files remain optional capability surfaces.
- Request-history inclusion is caller/editor policy. Local conversation storage is runtime client state.
- File attachment is request-scoped and OpenAI-Responses-specific in the current implementation.
- Pricing estimation is optional tooling support, not invoice-grade accounting.
- `BCS.LLM.Core.*` is the preferred naming; legacy `Eon.*` references are compatibility language only.

## Update map
- Runtime or provider behavior changes → `SSoT_Runtime_and_OpenAI_Provider.md`
- Wizard/editor behavior changes → `SSoT_Editor_Tooling_and_Wizard.md`
- Pricing rates / estimation semantics changes → `SSoT_Pricing_Pipeline.md`
- Cross-cutting config/env/instruction semantics → `SSoT_CONTRACTS.md`
- Authority changes / document moves / semantic reclassification → `coverage-matrix.md` + `changelog-ssot.md`
- Active milestone / priorities → `CURRENT_STATE.md` + roadmap

## Small-system variant note
This package intentionally uses a lightweight variant of the broader SSoT governance baseline:
- one roadmap file,
- three subsystem SSoTs,
- no dedicated research layer yet,
- no heavy folder nesting beyond planning/reference/archive.

That is deliberate. The current package documentation set is small enough that extra layers would add friction rather than clarity.
