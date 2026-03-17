# BCS LLM Core (Unity)

Provider-agnostic Unity ↔ LLM infrastructure package with:
- runtime client/config abstractions,
- an OpenAI provider,
- ScriptableObject-based agent/config assets,
- editor setup and testing tooling,
- optional file upload / Responses-with-files support,
- optional pricing estimation utilities,
- and a reusable prompt-workflow stack that now covers prompt build, contract hints, validation, retry classification, and minimal orchestration.

This README is the **repository/package overview**.
It is **not** the single source of truth for subsystem semantics or boundary decisions. For authoritative details, use the docs in `Documentation~/`.

---

## Current status

**Status date:** 2026-03-17

The package is currently in an **active but stable** state with the following roadmap phases completed:
- **Phase 1 — Prompt Builder interface extraction / generalization**
- **Phase 2 — Contract-aware prompt hints**
- **Phase 3 — Validation / repair extension points**
- **Phase 4 — Retry-oriented execution surfaces**
- **Phase 5 — Minimal orchestration pipeline**

**Phase 6 — Reassess semantic canonicalization** remains **deferred / evidence-gated**. It should only start if another project beyond APEC Watch demonstrates the same semantic identity / canonicalization pressure.

A key current property of the package is that **the minimal adoption path is still valid**:
- define an input DTO,
- implement `IPromptBuilder<TInput>`,
- return `InstructionsText` + `UserPromptText`,
- execute via `PromptExecutionHelper` or equivalent caller-side runtime code.

Validation, retry classification, and orchestration are **optional additive layers**, not mandatory complexity for every new agent.

---

## What the package contains

## Runtime layers

### 1) Client/config baseline
The runtime baseline includes provider-agnostic client/config contracts plus an OpenAI provider implementation.

Main areas:
- `Runtime/Clients/`
- `Runtime/OpenAI/`
- `Runtime/Integration/`
- `Runtime/Agents/`

Representative surfaces:
- `ILLMClient`
- `LLMClientData`
- `OpenAIClientData`
- `LLMClientFactory`
- `OpenAILLMClient`
- `LLMAgentData`
- `LLMAgentInstructionsData`
- `LLMEnvLoader`
- `LLMEnvSettings`

### 2) Prompt composition
The package includes a reusable runtime-facing prompt composition layer.

Main area:
- `Runtime/Prompts/`

Representative surfaces:
- `IPromptBuilder<TInput>`
- `PromptBuildMode`
- `PromptBuildContext`
- `PromptBuildResult`
- `PromptExecutionOptions`
- `PromptExecutionHelper`
- `PromptContractHint`
- `IContractHintProvider<TInput>`

Purpose:
- standardize prompt construction,
- preserve the logical separation between instructions and user prompt,
- support mode-specific builds,
- keep provider request shaping outside the builder.

### 3) Validation / repair extensions
The package includes reusable validation / repair reporting seams for workflows that need structured post-response analysis.

Main area:
- `Runtime/Validation/`

Representative surfaces:
- `ValidationSeverity`
- `ValidationTargetScope`
- `ValidationTarget`
- `ValidationIssue`
- `ValidationResult`
- `IResponseValidator<T>`
- `RepairHint`
- `IRepairHintProvider<T>`

### 4) Retry classification
The package includes a reusable retry-classification layer that can describe retryability and re-entry targets without owning domain-specific retry reconstruction.

Main area:
- `Runtime/Retry/`

Representative surfaces:
- `RetryDisposition`
- `RetryDirective`
- `IRetryClassifier<TContext>`

### 5) Minimal orchestration
The package now includes a minimal reusable orchestration layer for multi-step workflows.

Main area:
- `Runtime/Orchestration/`

Representative surfaces:
- `PromptWorkflowState<TInput>`
- `WorkflowAdvance`
- `WorkflowStepResult`
- `IWorkflowStep<TState>`
- `IWorkflowReentryAdapter<TState>`
- `WorkflowRunResult<TState>`
- `LinearWorkflowRunner<TState>`
- `PromptBuildStep<TState, TInput>`
- `PromptExecuteStep<TState, TInput>`

Purpose:
- coordinate small linear attempt pipelines,
- compose build / execute / validate / classify-retry / optional re-entry,
- keep orchestration bounded and runtime-facing,
- avoid becoming a workflow graph engine.

### 6) Pricing
The package also includes optional pricing-estimation tooling.

Main area:
- `Runtime/Pricing/`

Representative surfaces:
- `LLMModelPricingCatalogSO`
- `LLMPricingEstimator`
- `OpenAIPricingCatalogExtensions`

---

## Editor tooling

Main area:
- `Editor/`

Current editor surfaces include:
- `LLMAgentWizardWindow`
- `LLMEnvSetupWindow`
- `OpenAIPricingCatalogMenu`

The editor wizard remains the canonical local test harness for:
- agent selection,
- client rebuild,
- connectivity testing,
- prompt sending,
- usage display,
- optional cost estimation,
- history controls,
- and PDF upload flow.

---

## Architectural boundary summary

## Belongs in LLM Core
- provider-agnostic runtime contracts,
- provider implementations,
- prompt composition infrastructure,
- optional contract hints,
- optional validation / repair surfaces,
- optional retry-classification surfaces,
- optional minimal orchestration surfaces,
- editor tooling for setup/testing,
- pricing estimation utilities.

## Stays outside LLM Core
- project-specific prompt wording and schema meaning,
- project-specific validation semantics,
- deterministic domain autofix logic,
- project-specific retry payload reconstruction,
- candidate scoring / replacement policies,
- apply/upsert pipelines,
- project-specific semantic canonicalization/merge semantics,
- editor-only business UX from downstream projects.

This boundary matters because the package is intentionally evolving as a **small reusable runtime core**, not as a project-agnostic workflow engine.

---

## Quick start

1. Set `OPENAI_API_KEY` via local `.env` or OS environment.
2. Optionally create `LLMEnvSettings.asset` under `Assets/Resources/`.
3. Create `OpenAIClientData`, `LLMAgentInstructionsData`, and `LLMAgentData` assets.
4. Open **Tools → LLM → Agent Wizard (v0)**.
5. Rebuild the client, ping, and send a prompt.

For prompt-builder-based integrations, the smallest valid path is:
1. Create an agent-specific input DTO.
2. Implement `IPromptBuilder<TInput>`.
3. Return `InstructionsText` and `UserPromptText`.
4. Execute via `PromptExecutionHelper`.
5. Only adopt contract hints / validation / retry / orchestration if the workflow truly needs them.

---

## Important operational notes

- Secrets belong in environment configuration, not in assets.
- Base URL and endpoint settings are configuration, not secrets.
- File upload and file-attach behavior are optional capabilities, not assumptions of the base client contract.
- `PromptExecutionHelper` remains a **single-shot execution bridge** even after Phase 5.
- The orchestration layer is intentionally narrow:
  - no workflow graph engine,
  - no generic branching framework,
  - no shared apply/upsert pipeline,
  - no shared semantic canonicalization engine.
- Pricing estimates are approximate and intended for tooling, not billing reconciliation.

---

## Documentation map

Read these first:
- `Documentation~/README.md`
- `Documentation~/SSoT_INDEX.md`
- `Documentation~/CURRENT_STATE.md`

Primary authorities:
- Cross-cutting rules: `Documentation~/SSoT_CONTRACTS.md`
- Runtime + provider truth: `Documentation~/SSoT_Runtime_and_OpenAI_Provider.md`
- Editor/tooling truth: `Documentation~/SSoT_Editor_Tooling_and_Wizard.md`
- Pricing truth: `Documentation~/SSoT_Pricing_Pipeline.md`
- Planning / roadmap: `Documentation~/planning/Roadmap_LLM_Core.md`

Reference docs:
- `Documentation~/reference/Prompt_Builder_Implementation_Guide.md`
- `Documentation~/reference/LLM_Core_EditorWindow_Integration_Guide.md`
- `Documentation~/reference/llm-agent-wizard-test-cases.md`

Archive:
- superseded mixed-state docs and absorbed historical notes live under `Documentation~/archive/`.

---

## Notes on naming and maturity labels

- Package identifier: `com.bcs.llm-core`
- Some older docs/UI still use `v0` / `v0.1` language while package metadata is `1.0.0`.
- Some older projects may still use legacy `Eon.Narrative.LLM.*` naming.
- `BCS.LLM.Core.*` is the preferred terminology going forward.

---

## Practical reading guide

If you are:
- **integrating a new agent**, start with the Prompt Builder guide and the runtime/provider SSoT;
- **working on editor tooling**, start with the editor SSoT and editor integration guide;
- **changing core abstractions**, check `CURRENT_STATE.md`, the roadmap, and the update protocol expectations before editing docs.
