# Current State — BCS / Eon LLM Core

**Status date:** 2026-03-17

## Current true state
- The package has a provider-agnostic client/config core plus an OpenAI provider implementation.
- The editor wizard remains the canonical local test harness for agent selection, rebuild, ping, prompt sending, usage display, optional cost estimate, history controls, and PDF upload flow.
- File upload and Responses-with-files exist as explicit optional capabilities in code.
- The pricing pipeline exists as a real subsystem: catalog asset, estimation utility, default seeding helper, and editor menu action.
- A lightweight SSoT documentation baseline is in place for the package.
- Prompt Builder Phase 1 is implemented and validated as a reusable runtime-facing pattern.
- Prompt Builder Phase 2 is implemented and validated as a reusable contract-aware extension of that baseline.
- Prompt Builder Phase 3 is implemented and validated as a reusable validation / repair extension seam layered on top of the Prompt Builder baseline.
- Prompt Builder Phase 4 is implemented and validated as a reusable retry-classification layer layered above validation.
- Prompt Builder Phase 5 is now implemented and validated as a **minimal orchestration pipeline** layered above build / execute / validate / classify-retry.
- The current shared Prompt Builder + validation + retry + orchestration baseline now includes:
  - `IPromptBuilder<TInput>`
  - `PromptBuildResult`
  - `PromptBuildMode`
  - `PromptBuildContext`
  - `PromptExecutionHelper`
  - expanded `PromptContractHint`
  - optional `IContractHintProvider<TInput>`
  - `ValidationSeverity`
  - `ValidationTargetScope`
  - `ValidationTarget`
  - `ValidationIssue`
  - `ValidationResult`
  - `IResponseValidator<T>`
  - `RepairHint`
  - `IRepairHintProvider<T>`
  - `RetryDisposition`
  - `RetryDirective`
  - `IRetryClassifier<TContext>`
  - `PromptWorkflowState<TInput>`
  - `WorkflowAdvance`
  - `WorkflowStepResult`
  - `IWorkflowStep<TState>`
  - `IWorkflowReentryAdapter<TState>`
  - `WorkflowRunResult<TState>`
  - `LinearWorkflowRunner<TState>`
  - `PromptBuildStep<TState, TInput>`
  - `PromptExecuteStep<TState, TInput>`
- The first proven Phase 5 adoption path uses a narrow APEC Watch pilot around **single PDF extraction**, preserving the existing editor behavior while routing build → execute → parse/validate → classify-retry → optional re-entry through the new linear runner.
- Contract-aware hints remain runtime-facing prompt metadata, not provider request DTOs and not editor-only configuration.
- Shared validation / repair surfaces remain runtime-facing optional extension points, not provider request DTOs and not editor-only configuration.
- Shared retry-classification surfaces remain runtime-facing optional extension points, not provider request DTOs and not editor-only configuration.
- Shared orchestration surfaces are now runtime-facing optional coordination infrastructure, not provider request DTOs and not editor-only configuration.
- The documentation system explicitly includes a short update protocol rule and a minimum decision batch for determining which docs must be updated after technical changes.

## Active milestone
There is **no new extraction phase currently active**.

Phase 5 is considered complete. The current operating stance is:
- keep the first APEC Phase 5 pilot stable,
- close documentation/governance alignment for the landed orchestration surface,
- and keep **Phase 6 — Reassess semantic canonicalization** deferred until there is stronger cross-project evidence.

## Recently completed
- Prompt Builder Phase 5 was designed, implemented, and validated with:
  - a shared orchestration namespace in runtime,
  - typed workflow state,
  - a linear workflow-step contract,
  - a bounded linear coordinator with re-entry adapter support,
  - preservation of `PromptExecutionHelper` as a single-shot execution bridge,
  - and a first narrow APEC Watch adoption over single PDF extraction.
- The APEC Watch pilot proved that build → execute → parse/validate → classify-retry → optional re-entry can be composed without turning LLM Core into a workflow engine.
- Planning/current-state/runtime/editor/reference docs were updated to reflect that Phase 5 is complete and that Phase 6 remains deferred / evidence-gated.

## Short horizon
- Keep the first APEC Phase 5 pilot stable.
- Confirm there is no pressure to widen Phase 5 into apply/upsert orchestration.
- Keep runtime/contracts/editor docs synchronized with the landed orchestration boundary.
- Continue using the Prompt Builder guide as the implementation baseline for new agent-specific builders.
- Keep the minimal builder path explicit so future agents are not forced into orchestration layers prematurely.

## Medium horizon
- Add debug request diagnostics in editor tooling.
- Improve files panel UX and status feedback.
- Re-evaluate whether reflection fallback is still needed once older/custom integrations are migrated.
- Watch for a second project/agent that demonstrates truly shared semantic canonicalization pressure.

## Long horizon
- Expand provider support only when there is real code pressure to do so.
- Add more subsystem docs only if the package surface genuinely grows.
- Only generalize more of the extraction/retry/orchestration pipeline if at least one more agent demonstrates the same pressure.
- Reassess semantic canonicalization only when there is enough cross-project evidence to justify it.

## Known deltas / watch items
- Package metadata says `1.0.0`, while some docs/UI still use `v0` / `v0.1` language. This remains the main visible naming/maturity-label mismatch to clean up.
- The Prompt Builder surface is intentionally small. It should not silently absorb provider file IDs, editor-only policy, or domain-specific validation/canonicalization logic without stronger cross-project evidence.
- `PromptContractHint` is no longer only conceptual; it now participates in the build path. The main remaining guardrail is to prevent builders from drifting into generic schema-renderer territory.
- `IContractHintProvider<TInput>` is currently a valid reusable pattern, but it should remain optional until more than one project confirms the exact same pressure.
- The validation / repair surfaces are intentionally descriptive and composable. They should not silently absorb deterministic autofix logic, project-specific apply/block semantics, or retry orchestration policy without stronger cross-project evidence.
- The retry-classification surfaces are intentionally narrow. They should not silently absorb provider transport retry policy or domain-shaped prompt-input reconstruction without stronger cross-project evidence.
- The orchestration surfaces are intentionally narrow. They should not silently absorb apply/upsert, candidate scoring, branching workflow-engine semantics, or project-specific JSON splice/remediation logic without stronger cross-project evidence.
- A minimal agent integration should remain viable with only:
  - input DTO,
  - `IPromptBuilder<TInput>`,
  - `PromptBuildResult`,
  - and execution via `PromptExecutionHelper` or equivalent caller-side runtime code.

Validation, retry classification, and orchestration remain optional and additive.

## Next edit points
- `SSoT_Runtime_and_OpenAI_Provider.md`
- `SSoT_CONTRACTS.md`
- `planning/Roadmap_LLM_Core.md`
- `changelog-ssot.md`
- `CURRENT_STATE.md`
- `coverage-matrix.md`
- `reference/Prompt_Builder_Implementation_Guide.md`
- `reference/LLM_Core_EditorWindow_Integration_Guide.md`
- `SSoT_Editor_Tooling_and_Wizard.md`
