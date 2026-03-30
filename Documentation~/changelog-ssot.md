## 2026-03-29 — OpenAI model selector and pricing-pipeline alignment
- Updated `SSoT_Runtime_and_OpenAI_Provider.md` to record the current `OpenAIClientData` model-selector policy:
  - preserve legacy enum members for Unity serialization safety,
  - keep current and compatibility IDs conceptually distinct,
  - and explicitly distinguish `gpt-5-mini` / `gpt-5-nano` from `gpt-5.4-mini` / `gpt-5.4-nano`.
- Updated `SSoT_Pricing_Pipeline.md` to clarify that OpenAI pricing bootstrap coverage must stay aligned with the exact `modelId` strings exposed by `OpenAIClientData`, not just a loosely similar family name.
- Added a practical pricing refresh / usage loop to `SSoT_Pricing_Pipeline.md` so editor/tool users can re-seed and verify pricing coverage without re-learning the subsystem from code.
- Recorded current OpenAI pricing caveats in `SSoT_Pricing_Pipeline.md` for long-context `gpt-5.4` / `gpt-5.4-pro`, regional-processing uplift on the GPT-5.4 family mini/nano/pro entries, and the cached-input = `0` bootstrap rule when the official source does not list a cached-input price.
- Updated `reference/llm-agent-wizard-test-cases.md` with explicit pricing regression checks for catalog precedence, client fallback, and the no-pricing path.

# Semantic Changelog — BCS / Eon LLM Core

## 2026-03-17 — Prompt Builder Phase 5 / Minimal orchestration pipeline
- Updated `planning/Roadmap_LLM_Core.md` to mark **Phase 5 — Minimal orchestration pipeline** as completed and to keep **Phase 6 — Reassess semantic canonicalization** deferred / evidence-gated.
- Updated `CURRENT_STATE.md` to record that the shared baseline now includes optional orchestration surfaces and that there is no new extraction phase currently active beyond stabilizing the first APEC pilot.
- Updated `SSoT_CONTRACTS.md` to add shared orchestration terms and invariants for:
  - workflow state,
  - workflow step,
  - workflow re-entry adapter,
  - linear workflow runner,
  - and the rule that orchestration remains narrow, linear, and separate from apply/remediation semantics.
- Updated `SSoT_Runtime_and_OpenAI_Provider.md` to clarify that:
  - a minimal orchestration layer now exists in runtime,
  - `PromptExecutionHelper` remains a single-shot execution helper,
  - and the first proven orchestration adoption is a narrow APEC Watch single-extraction pilot.
- Updated `SSoT_Editor_Tooling_and_Wizard.md` to record that editor tooling still owns entrypoint selection, busy state, status UX, and local workflow bridging even when shared orchestration is used.
- Updated `SSoT_INDEX.md` and `coverage-matrix.md` to map orchestration semantics and editor bridge responsibilities to the correct authorities.
- Updated `reference/Prompt_Builder_Implementation_Guide.md` to make the Tier 5 orchestrated flow explicit as an optional later adoption tier rather than a prerequisite.
- Updated `reference/LLM_Core_EditorWindow_Integration_Guide.md` to capture the narrow Phase 5 bridge pattern from an EditorWindow into `LinearWorkflowRunner`.

## 2026-03-17 — Prompt Builder Phase 4 / Retry-classification extension points
- Updated `planning/Roadmap_LLM_Core.md` to mark **Phase 4 — Retry-oriented execution surfaces** as completed and **Phase 5 — Minimal orchestration pipeline** as the next active phase.
- Updated `CURRENT_STATE.md` to record that the shared Prompt Builder baseline now includes optional retry-classification surfaces and that the next active milestone is Phase 5.
- Updated `SSoT_CONTRACTS.md` to add the shared retry terms and invariants for:
  - `RetryDisposition`,
  - `RetryDirective`,
  - `IRetryClassifier<TContext>`,
  - reuse of `ValidationTarget` for retry scope/target description,
  - and the boundary that retry classification remains separate from orchestration and domain-shaped prompt-input reconstruction.
- Updated `SSoT_Runtime_and_OpenAI_Provider.md` to clarify that:
  - `PromptExecutionHelper` remains a single-shot execution helper,
  - Phase 4 does not introduce a shared retry orchestrator,
  - APEC Watch maps local retry evidence into shared retry directives,
  - and runtime retry surfaces remain optional for minimal agent integrations.
- Updated `SSoT_Editor_Tooling_and_Wizard.md` to record that editor tooling still owns manual retry UX/state and that shared retry directives may be bridged back into local prompt-builder calls without introducing a generic retry UI.
- Updated `SSoT_INDEX.md` and `coverage-matrix.md` to map retry-classification semantics and their runtime/editor boundaries to the correct primary authorities.
- Updated `reference/Prompt_Builder_Implementation_Guide.md` to clarify the Phase 4 usage pattern:
  - builders still consume retry through `PromptBuildContext` and build modes,
  - retry directives classify re-entry but do not replace domain input DTOs,
  - and targeted retry payload reconstruction remains project-owned.
- Updated `reference/LLM_Core_EditorWindow_Integration_Guide.md` to capture the editor-side bridge pattern from shared retry directives back into local prompt-builder calls.
- Recorded the implemented Phase 4 output pattern:
  - shared retry disposition / directive / classifier contracts,
  - reuse of `ValidationTarget` for retry target/scope description,
  - APEC-owned retry classification for pack-targeted and rule-targeted retry,
  - local retry-directive → prompt-builder bridge integration,
  - and preservation of the minimal builder-first adoption path.

## 2026-03-17 — Prompt Builder Phase 3 / Validation and repair extension points
- Updated `planning/Roadmap_LLM_Core.md` to mark **Phase 3 — Validation / repair extension points** as completed and **Phase 4 — Retry-oriented execution surfaces** as the next active phase.
- Updated `CURRENT_STATE.md` to record that the Prompt Builder baseline now includes optional shared validation / repair surfaces and that the next active milestone is Phase 4.
- Updated `SSoT_INDEX.md` to record the new invariant that validation / repair surfaces are optional runtime-facing extension points and that a minimal agent integration must remain possible without adopting advanced layers.
- Updated `coverage-matrix.md` to map the new validation / repair concept area and to make the minimal builder adoption path more explicit in documentation ownership.
- Updated `reference/Prompt_Builder_Implementation_Guide.md` to:
  - make the minimal builder path explicit as the default/simple adoption path,
  - clarify that contract hints, validators, repair hints, and later retry/orchestration layers are optional,
  - add a progressive adoption model,
  - and keep the boundary clear between minimal prompt composition and optional advanced lifecycle surfaces.
- Recorded the implemented Phase 3 output pattern:
  - shared validation severity / targeting / issue / result surfaces,
  - reusable `IResponseValidator<T>`,
  - optional `RepairHint` / `IRepairHintProvider<T>`,
  - APEC Watch adapters over raw-input, bundle-parse, and persisted-database validation,
  - additive editor-side shared validation mirroring rather than replacement of domain-owned validation logic.
- Recorded the governance stance that Phase 3 does **not** make validation or repair mandatory for new agents. A minimal `IPromptBuilder<TInput>` + execution path remains valid.

## 2026-03-17 — Prompt Builder Phase 2 / Contract-aware prompt hints
- Updated `SSoT_CONTRACTS.md` to promote contract-aware prompt hints from a conceptual placeholder into current cross-cutting truth.
- Updated `SSoT_Runtime_and_OpenAI_Provider.md` to record the Phase 2 prompt-composition/runtime boundary:
  - `PromptBuildContext` now carries `ContractHint`,
  - structured contract hints are builder-consumable runtime-facing metadata,
  - `IContractHintProvider<TInput>` is an optional reusable pattern,
  - `PromptExecutionHelper` responsibility did not expand.
- Updated `planning/Roadmap_LLM_Core.md` to mark **Phase 2 — Contract-aware prompt hints** as completed.
- Updated `CURRENT_STATE.md` to record that Phase 2 is now implemented and that the next active milestone is **Phase 3 — Validation / repair extension points**.
- Updated `reference/Prompt_Builder_Implementation_Guide.md` to add Phase 2 implementation guidance for:
  - `PromptContractHint`,
  - `IContractHintProvider<TInput>`,
  - builder-consumed contract metadata,
  - and the rule that builders must not become generic schema renderers.
- Recorded the implemented Phase 2 output pattern:
  - expanded `PromptContractHint`,
  - structured contract object/field/token-set hints,
  - optional `IContractHintProvider<TInput>`,
  - agent-owned schema/DTO/enum → hint mapping,
  - builder consumption of contract hints for anti-drift scaffolding.
- Recorded that the APEC Watch migration again used prompt-text A/B comparison as the primary non-regression guardrail, and that the manual comparison path passed.

## 2026-03-16 — Prompt Builder Phase 1 documentation update
- Updated `SSoT_Runtime_and_OpenAI_Provider.md` to record the new runtime-facing prompt-composition layer and its boundary relative to `ILLMClient`, `ILLMResponsesFileClient`, and OpenAI API variants.
- Updated `SSoT_CONTRACTS.md` to define `InstructionsText`, `UserPromptText`, prompt build modes, prompt artifact hints, and the builder/executor boundary as cross-cutting package terms.
- Updated `CURRENT_STATE.md` to record that Prompt Builder Phase 1 now exists and is the current adoption focus for new agents.
- Updated `SSoT_INDEX.md` to include a local documentation-update protocol note and the new Prompt Builder reference guide.
- Updated `coverage-matrix.md` to map Prompt Builder concepts and the new implementation guide to their authoritative homes.
- Added `reference/Prompt_Builder_Implementation_Guide.md` as the implementation/reference guide for creating new agent-specific prompt builders.
- Updated `Documentation_Update_Protocol_Addendum.md` with a short decision rule and minimum-file-set guidance for determining which docs need updates after a technical change.

## 2026-03-16 — Post-fix documentation alignment
- Updated `CURRENT_STATE.md` to reflect that the env-fallback and API-key-redaction issues were resolved in code.
- Updated `SSoT_CONTRACTS.md` to promote the env-fallback rule and the “redact secrets in debug-oriented strings” rule into current cross-cutting truth.
- Updated `SSoT_Runtime_and_OpenAI_Provider.md` to replace the old `allowOsEnvFallback` delta with the implemented runtime behavior.
- Updated `SSoT_Editor_Tooling_and_Wizard.md` to record the current stance:
  - explicit capability interface first,
  - reflection only as compatibility fallback.
- Recommended adding a short documentation-update protocol note to `SSoT_INDEX.md` or `Documentation~/README.md` so the maintenance loop is visible inside the package docs themselves.

## 2026-03-16 — Documentation governance migration
- Introduced a lightweight SSoT governance baseline under `Documentation~/`.
- Split previous mixed documentation responsibilities into:
  - `CURRENT_STATE.md`
  - `planning/Roadmap_LLM_Core.md`
  - `SSoT_Runtime_and_OpenAI_Provider.md`
  - `SSoT_Editor_Tooling_and_Wizard.md`
  - `SSoT_Pricing_Pipeline.md`
- Reclassified the EditorWindow integration guide as reference documentation.
- Reclassified manual wizard tests as validation/reference documentation.
- Archived the previous mixed state-plan document.
- Absorbed the previous pricing explainer into a pricing subsystem SSoT.
- Added explicit authority mapping via `coverage-matrix.md`.

## 2026-03-16 — Newly explicit implementation truths captured
- Base runtime contract remains `ILLMClient`.
- File upload and Responses-with-files are optional capability interfaces, not base-contract features.
- Wizard history suppression is an editor policy layered on top of runtime history storage.
- Effective instructions precedence was recorded explicitly.
- Pricing estimate precedence was recorded explicitly: catalog first, then client fallback.
- The `allowOsEnvFallback` settings field was originally documented as a current implementation/documentation delta and was later resolved in the post-fix alignment update above.
- The API-key exposure in `LLMClientData.ToString()` was originally documented as a watch item and was later resolved in the post-fix alignment update above.
