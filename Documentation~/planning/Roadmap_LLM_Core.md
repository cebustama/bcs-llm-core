# Roadmap — BCS / Eon LLM Core

**Status date:** 2026-03-17  
**Status:** Active  
**Authority:** Planning / Implementation Roadmap  
**Primary context:** Prompt Builder extraction from APEC Watch into reusable LLM Core infrastructure

---

## Current roadmap status summary

- **Phase 1 — Prompt Builder interface extraction / generalization:** **Completed**
- **Phase 2 — Contract-aware prompt hints:** **Completed**
- **Phase 3 — Validation / repair extension points:** **Completed**
- **Phase 4 — Retry-oriented execution surfaces:** **Completed**
- **Phase 5 — Minimal orchestration pipeline:** **Completed**
- **Phase 6 — Reassess semantic canonicalization:** Deferred until enough cross-project evidence exists

---

## Why Phase 1 can be considered complete

Phase 1 can now be considered complete because the intended minimum implementation has been achieved without breaking the existing APEC Watch behavior:

- `IPromptBuilder<TInput>` and the core prompt result/context types were introduced.
- A concrete **APEC Watch adapter** was added so the new abstraction is exercised by a real agent/project surface.
- Prompt composition still preserves the logical separation between **instructions** and **user prompt**.
- The shape supports the intended build modes used by the extraction workflow:
  - `Default`
  - `Batch`
  - `TargetedRetry`
- The abstraction remains **runtime-facing** and does not introduce a hard dependency on editor-only code or provider-only code.
- The A/B prompt comparison added to the editor flow passed, which is the key non-regression signal for this phase.

### Phase 1 implementation note

The practical Phase 1 outcome is slightly stronger than originally scoped, because the phase now includes not only prompt composition contracts, but also a minimal execution bridge:

- prompt composition via `IPromptBuilder<TInput>`
- execution reuse via `PromptExecutionHelper`

This is still considered valid for Phase 1 because it did **not** collapse boundaries:
- prompt composition remains separate from provider execution,
- attachment handling remains an execution concern,
- history policy remains caller/editor-driven rather than builder-driven.

### Phase 1 operating rule that remains true after later phases

A **minimal Prompt Builder** is still a valid first-class adoption path.

A new agent does **not** need to implement:
- contract hints,
- validators,
- repair hints,
- retry classification,
- or orchestration layers

just to send prompts and receive responses.

The minimum valid path is still:

- define an agent-specific input DTO,
- implement `IPromptBuilder<TInput>`,
- return `InstructionsText` + `UserPromptText`,
- execute with `PromptExecutionHelper` or equivalent caller-side execution.

Later phases add optional extension points. They do not invalidate the minimal builder path.

---

## Why Phase 2 can be considered complete

Phase 2 can now be considered complete because the intended contract-aware anti-drift extraction landed without collapsing the Phase 1 boundary:

- `PromptContractHint` became a real build-path input rather than a placeholder concept.
- A structured contract-hint shape now exists for object/field/token-set/hard-rule metadata.
- An optional reusable `IContractHintProvider<TInput>` pattern was introduced.
- APEC Watch now maps schema/DTO/enums into contract hints through an agent-owned provider.
- The builder consumes contract hints for anti-drift scaffolding while preserving domain-owned prompt wording and mapping semantics.
- Prompt A/B comparison again passed in the editor migration flow, which is the key non-regression signal for this phase.

### Phase 2 implementation note

The practical Phase 2 outcome is intentionally narrow:

- contract hints now describe prompt-facing shape and constraints,
- the builder can render those hints into anti-drift scaffolding,
- provider execution surfaces did not expand,
- `PromptExecutionHelper` responsibility did not grow,
- domain truth still lives in project-owned schema/DTO/enums and mapping logic.

This is considered valid because Phase 2 extracted reusable contract metadata patterns **without** turning LLM Core into a generic schema engine.

### Phase 2 operating rule that remains true after later phases

Contract-aware prompt hints are **optional**.

Use them when:
- they remove duplicated anti-drift metadata,
- more than one prompt mode needs the same structural reminders,
- or the builder needs exact prompt-facing field/token constraints.

Do **not** require them for a minimal agent integration.

---

## Why Phase 3 can now be considered complete

Phase 3 can now be considered complete because the shared validation / repair seam was implemented and validated without collapsing Phase 1 or Phase 2 boundaries:

- shared validation contracts now exist in LLM Core,
- shared severity supports at least:
  - `Warning`
  - `Error`
  - `Blocker`
- APEC Watch can map real workflow artifacts into the shared validation surface:
  - raw JSON input,
  - bundle-parse output,
  - persisted database validation
- item/location-aware targeting is supported through shared validation-target metadata,
- duplicate semantic RuleKey detection can now surface as shared blocking validation state,
- the editor integration adopted the new shared validation layer as a **mirror** over the existing flow,
- existing APEC validation / autofix / parser / quality-gate semantics remained intact,
- compile and validation smoke tests passed.

### Phase 3 implementation note

The implemented Phase 3 pattern is intentionally narrow:

- shared core now describes **validation lifecycle surfaces**,
- APEC Watch still owns:
  - domain validation semantics,
  - deterministic autofix logic,
  - semantic canonicalization / RuleKey meaning,
  - extraction-specific gating semantics,
  - database-specific rule integrity checks.

This is considered valid because the reusable extraction was the **validation / repair reporting seam**, not APEC Watch’s fish-diagnostic business meaning.

---

## Phase 1 — Prompt Builder interface extraction / generalization

**Status:** Completed

### Objective
Introduce a reusable prompt-building abstraction without breaking current behavior.

### Completed DoD
- [x] `IPromptBuilder<TInput>` exists, along with prompt context/result support types.
- [x] An APEC Watch adapter implements the abstraction.
- [x] Logical separation between instructions and user prompt is preserved.
- [x] The abstraction supports `Default`, `Batch`, and `TargetedRetry` build modes.
- [x] The implementation does not introduce a hard dependency on editor-only or provider-only code.
- [x] A/B prompt comparison passed inside the APEC Watch editor flow.

### Implemented outputs
- Prompt contracts:
  - `IPromptBuilder<TInput>`
  - `PromptBuildMode`
  - `PromptBuildContext`
  - `PromptBuildResult`
  - `PromptRetryContext`
  - `PromptContractHint`
  - `PromptArtifactHint`
- Execution support:
  - `PromptExecutionOptions`
  - `PromptExecutionHelper`
- APEC Watch bridge:
  - `DiagnosticRulePromptBuildInput`
  - `DiagnosticRulePromptBuilderAdapter`
- Editor-side validation support:
  - backend selection (`Legacy` / `Adapter`)
  - local A/B prompt comparison
  - optional block-on-mismatch safety guard

### Boundary confirmed in Phase 1

**Belongs in LLM Core:**
- prompt composition abstractions
- prompt build result/context types
- minimal runtime execution bridge

**Belongs in agent/project code:**
- concrete prompt builder implementations
- domain-specific build inputs
- domain-specific prompt text and schema meaning

**Stays outside Prompt Builder:**
- provider upload and file-id resolution
- history inclusion policy
- validation / repair / canonicalization logic
- editor-specific UX and test surfaces

---

## Phase 2 — Contract-aware prompt hints

**Status:** Completed

### Objective
Decouple anti-drift behavior from domain-specific hardcoding.

### Completed DoD
- [x] `PromptContractHint` is used as a meaningful part of the build path rather than being only a placeholder type.
- [x] APEC Watch can populate contract hints from its schema/DTO/enums.
- [x] Builders can consume contract hints instead of requiring all structure to be hardcoded inline in builder code.
- [x] The resulting pattern remains provider-agnostic and editor-agnostic.
- [x] The implementation preserves the Phase 1 boundary: provider request generation, file attachment, and history policy remain outside the builder.
- [x] Prompt A/B comparison passed inside the APEC Watch editor flow after the Phase 2 adoption.

### Implemented outputs
- Prompt contracts:
  - expanded `PromptContractHint`
  - structured contract object/field/token-set/hard-rule helper types
  - optional `IContractHintProvider<TInput>`
- APEC Watch contract-hint adoption:
  - `DiagnosticRulePromptContractHintProvider`
  - agent-owned schema/DTO/enums → hint mapping
  - adapter-driven hint resolution
  - builder consumption of hint data for contract-aware anti-drift sections
- Editor-side validation support:
  - continued backend selection (`Legacy` / `Adapter`)
  - continued local A/B prompt comparison
  - successful manual A/B pass after the contract-hint adoption

### Boundary confirmed in Phase 2

**Belongs in LLM Core:**
- contract-hint prompt surfaces
- optional hint-provider pattern
- shared prompt-composition semantics for contract-aware anti-drift metadata

**Belongs in agent/project code:**
- mapping from project authority sources into contract hints
- domain-specific prompt wording and semantic interpretation
- domain-specific extraction/mapping rules

**Stays outside Prompt Builder:**
- provider upload and file-id resolution
- history inclusion policy
- validation / repair / canonicalization logic
- generic schema rendering as a shared runtime responsibility
- editor-specific UX and test surfaces

---

## Phase 3 — Validation / repair extension points

**Status:** Completed

### Objective
Extract the reusable pattern of:

**structured response → validate → detect problems → optionally provide repair guidance → prepare for later retry/re-entry**

without moving APEC Watch’s domain-specific validation semantics into shared LLM Core.

### Completed DoD
- [x] Shared validation / repair hook surfaces exist.
- [x] Validation results distinguish at least:
  - `Warning`
  - `Error`
  - `Blocker`
- [x] Shared validation targeting supports:
  - whole-response targeting
  - item-level targeting
  - field/location-aware targeting
- [x] APEC Watch can plug real workflow artifacts into the shared validation surface without moving fish-diagnostic business logic into LLM Core.
- [x] Shared validation is integrated into the real APEC Watch editor flow as an additive mirror rather than a semantic rewrite.
- [x] Existing APEC validation / autofix / parser / quality-gate flow remained behaviorally intact in tests.
- [x] The repair surface remains optional and composable.

### Implemented outputs
- Shared LLM Core validation surfaces:
  - `ValidationSeverity`
  - `ValidationTargetScope`
  - `ValidationTarget`
  - `ValidationIssue`
  - `ValidationResult`
  - `IResponseValidator<T>`
  - `RepairHint`
  - `IRepairHintProvider<T>`
- APEC Watch adapter adoption:
  - raw-input validation adapter
  - bundle-parse validation adapter
  - persisted-database validation adapter
- Editor-side adoption:
  - shared validation mirror state
  - additive shared-validation display
  - no replacement of legacy validation reports or quality-gate semantics

### Boundary confirmed in Phase 3

**Belongs in LLM Core:**
- shared validation-result surface
- shared issue-severity model
- shared validation targeting model
- optional repair-hint surface
- reusable validator interface

**Belongs in agent/project code:**
- domain validation semantics
- deterministic autofix / canonicalization
- semantic key construction and meaning
- project-specific retry semantics
- project-specific apply/block rules

**Stays outside shared Phase 3 core:**
- business/domain meaning of validation rules
- semantic merge logic
- provider execution policy
- retry orchestration policy
- editor-only workflow semantics

### Follow-up after Phase 3

The next step is no longer “prove validation hooks are reusable.” That has already been proven.

The next step is to formalize:

**Phase 4 — Retry-oriented execution surfaces**

so runtime code can decide, in a reusable way:
- whether retry is appropriate,
- what kind of retry is appropriate,
- and what retry context should flow back into prompt-building and execution.

---

## Phase 4 — Retry-oriented execution surfaces

**Status:** Completed

### Objective
Formalize reusable retry classification and retry-directive shapes around post-validation / post-gate failures without absorbing project-specific retry semantics into shared LLM Core.

### Completed DoD
- [x] Shared retry contracts exist in LLM Core:
  - `RetryDisposition`
  - `RetryDirective`
  - `IRetryClassifier<TContext>`
- [x] Shared retry contracts reuse the existing Phase 3 targeting model (`ValidationTarget`) rather than introducing a second target model.
- [x] The shared retry model can express:
  - no retry
  - retryable
  - terminal / do-not-retry
- [x] The shared retry model can describe retry targets at:
  - whole-response scope
  - item scope
  - field scope (via reused `ValidationTarget`)
- [x] Retry directives can suggest prompt re-entry mode without taking over prompt construction.
- [x] `PromptExecutionHelper` remains single-shot and unchanged in responsibility.
- [x] APEC Watch now maps its current retry families into the new surface:
  - pack-targeted retry
  - rule-targeted retry
  - terminal confirmed-null rule state
- [x] APEC Watch retry classification remains agent/project-owned:
  - quality-gate semantics stay local
  - lesion metrics stay local
  - candidate scoring stays local
  - JSON splice/apply stays local
- [x] Minimal agent integrations remain valid without adopting retry classification.

### Implemented outputs
- Shared LLM Core retry surfaces:
  - `RetryDisposition`
  - `RetryDirective`
  - `IRetryClassifier<TContext>`
- APEC Watch adoption:
  - local retry-classification context
  - local retry classifier
  - local retry-directive → prompt-builder bridge
  - rule-targeted retry wired through retry directives
  - pack-targeted retry wired through retry directives
- Documentation updates:
  - roadmap/current-state alignment for Phase 4 completion

### Boundary confirmed in Phase 4

**Belongs in LLM Core:**
- shared retry disposition model
- shared retry directive surface
- reusable retry-classifier interface
- reuse of `ValidationTarget` for retry scope/target description

**Belongs in agent/project code:**
- mapping from validation/gate evidence into retry classification input
- project-specific retry reason semantics
- targeted retry payload reconstruction
- candidate scoring / replacement policy
- terminal business meaning of confirmed drops

**Stays outside shared Phase 4 core:**
- retry orchestration loops
- provider transport retry policy
- project-specific JSON splice/apply semantics
- editor-only retry UX/state
- semantic canonicalization logic

### Implementation note
The Phase 4 extraction is intentionally narrow:
- shared core classifies retryability and describes re-entry,
- agent/project code still reconstructs any domain-shaped retry payload,
- prompt builders continue consuming retry through existing `PromptBuildContext` and build modes,
- no shared retry orchestrator was introduced.

This keeps advanced retry optional and preserves the minimal builder-first adoption path.

---

## Phase 5 — Minimal orchestration pipeline

**Status:** Completed

### Objective
Introduce a **small reusable runtime orchestration surface** for multi-step LLM workflows, so that:

- prompt build,
- execute,
- validate,
- classify retry,
- and optional re-entry

can be composed as a reusable **linear attempt pipeline** without editor coupling and without turning LLM Core into a workflow engine.

### Design rule
This phase is intentionally narrow.

It should extract only the smallest reusable orchestration pattern justified by the current APEC Watch evidence:

- a shared typed workflow state,
- a linear ordered step chain,
- a bounded coordinator,
- optional re-entry,
- and no graph engine / branching DSL / editor-centered orchestration.

### Completed understanding carried into Phase 5
The boundary from earlier phases remains:

- Prompt Builder builds prompt payloads.
- `PromptExecutionHelper` remains a **single-shot execution bridge**.
- Validation remains a reusable reporting/adaptation layer.
- Repair hints remain optional.
- Retry classification remains a reusable classification layer.
- Retry payload reconstruction stays agent-owned.
- Candidate scoring / replacement / apply semantics stay project-owned.
- Editor UX remains editor-owned.

### Concrete DoD
- [x] A minimal runtime orchestration namespace/surface exists in LLM Core.
- [x] A shared typed attempt-state base exists (for example `PromptWorkflowState<TInput>`).
- [x] A linear workflow-step contract exists (for example `IWorkflowStep<TState>`).
- [x] A bounded linear coordinator exists (for example `LinearWorkflowRunner<TState>`) with `MaxAttempts`.
- [x] The coordinator supports re-entry **only** through an agent-owned/project-owned adapter and does not reconstruct retry inputs by itself.
- [x] `PromptExecutionHelper` remains single-shot and is **not** expanded into a full orchestrator.
- [x] The Phase 5 surface supports composing:
  - build
  - execute
  - validate
  - classify retry
  - optional re-entry
- [x] Validation is allowed to appear at multiple points in the chain rather than as a single hardcoded slot.
- [x] Retry classification remains separate from:
  - candidate application
  - splice/replace semantics
  - apply/upsert logic
  - persisted DB remediation
- [x] No generic branching/DAG/workflow DSL/editor-graph engine is introduced.
- [x] APEC Watch can map at least one real subflow onto the new surface without breaking the completed Phase 1–4 baseline.
- [x] Minimal agents remain valid without adopting orchestration; `IPromptBuilder<TInput>` + `PromptExecutionHelper` is still a first-class minimal path.

### Likely deliverables
- `Runtime/Orchestration/` surface
- minimal attempt-state contract
- minimal workflow-step contract
- minimal workflow result / control model
- bounded linear coordinator
- optional agent-owned re-entry adapter contract
- first APEC Watch adoption on a narrow real subflow

### Explicit non-goals
- no workflow graph engine
- no generic branching framework
- no node editor
- no resumable workflow runtime
- no shared candidate scoring framework
- no shared apply/upsert pipeline
- no project-agnostic semantic merge/canonicalization engine

### Implementation note
The likely first safe adoption is **not** replacing the whole APEC Watch editor workflow.

The preferred first adoption is a narrow real flow such as:

- build
- execute
- parse/validate/gate
- classify retry
- optional re-entry

while keeping outside shared orchestration:

- JSON splice/replace
- candidate comparison
- apply plan
- persisted DB apply/revalidation
- editor buttons/state/progress UX

---

## Phase 6 — Reassess semantic canonicalization

**Status:** Deferred / Evidence-gated

### Objective
Decide whether there is enough evidence across projects to extract semantic identity / canonicalization / merge patterns.

### DoD
- [ ] There is a comparison with at least one additional agent/project beyond APEC Watch.
- [ ] Repetition is real enough to justify extraction.
- [ ] If repetition is weak or domain-specific, the logic remains project-owned.

### Why this phase is deferred
Canonicalization and merge semantics are highly domain-shaped. They should only move into LLM Core if multiple projects truly need the same abstraction.

---

## Progressive adoption rule for future agents

A new agent does **not** need to adopt all phases at once.

Valid adoption tiers are:

### Tier 1 — Minimal builder
- input DTO
- `IPromptBuilder<TInput>`
- `PromptBuildResult`
- separate execution via `PromptExecutionHelper` or equivalent

### Tier 2 — Contract-aware builder
- Tier 1
- optional `PromptContractHint`
- optional `IContractHintProvider<TInput>`

### Tier 3 — Validated flow
- Tier 2 or Tier 1
- optional shared validation surfaces
- optional shared repair-hint surfaces

### Tier 4 — Retry-aware flow
- Tier 3 or earlier minimal tiers
- optional retry classification
- optional retry directives
- optional retry-aware prompt re-entry

### Tier 5 — Orchestrated multi-step flow
- any earlier tier as needed
- optional linear orchestration surface
- optional shared attempt-state coordination
- optional bounded re-entry loop

This tiered model is important: later phases add reusable capabilities, but they do not make the minimal builder path invalid.

---

## Next batch guidance

### Batch A — Phase 1 precision
**Status:** Completed

Used to confirm how prompt composition should meet the actual runtime execution surface.

Completed inputs:
- `ILLMClient.cs`
- `ILLMFileClient.cs`
- `ILLMResponsesFileClient.cs`
- `OpenAILLMClient.cs`
- `LLMAgentWizardWindow.cs`

### Batch B — Phase 2 design and implementation
**Status:** Completed

Completed inputs:
- `DiagnosticRuleSchema.cs`
- `DiagnosticRuleJsonDtos.cs`
- `DiagnosticRulePackType.cs`
- `DiagnosticRulePromptBuildInput.cs`
- relevant enums such as:
  - `Species`
  - `NumericComparisonType`
  - schema-linked extraction enums

**Purpose:**  
Design and implement a precise contract-hint strategy without over-generalizing APEC Watch domain truth.

### Batch C — Phase 3 design and implementation
**Status:** Completed

Completed inputs:
- `DiagnosticRuleValidator.cs`
- `DiagnosticRuleJsonAutoFixer.cs`
- `DiagnosticRuleJsonBundleParser.cs`
- `DiagnosticRuleJsonParser.cs`
- `DiagnosticRuleKey.cs`
- `DiagnosticRuleDatabase.cs`
- `DiagnosticRuleEditorWindow` partials
- ruleset asset types

**Purpose:**  
Ground the shared validation / repair seam in the real APEC Watch pipeline and verify it without replacing domain-owned validation semantics.

### Batch D — Phase 4 design and implementation
**Status:** Completed

Completed inputs:
- retry / re-entry call sites in APEC Watch
- quality-gate / retry-decision logic
- retry prompt shaping helpers
- UI/runtime code that decides between whole retry vs targeted retry

**Purpose:**  
Design and implement reusable retry-oriented execution surfaces grounded in real post-validation decision points.

### Batch E — Phase 5 design and implementation
**Status:** Completed

Completed inputs:
- `ILLMClient.cs`
- any current APEC runtime/service file that already performs a multi-step extraction flow outside the editor, if such a file exists
- first narrow APEC adoption target for orchestration (recommended: single extraction or pack-targeted retry attempt)

**Purpose:**  
Implement the smallest viable reusable orchestration layer:
- typed attempt state
- linear step contract
- bounded coordinator
- agent-owned re-entry adapter
- first narrow APEC Watch adoption without absorbing domain semantics into LLM Core

---

## Immediate next step

The immediate next step is **not** to begin another extraction phase automatically.

Phase 5 is now considered complete. The next responsible step is to:

1. close documentation/governance around the landed Phase 5 behavior,
2. keep the first APEC pilot stable,
3. and hold Phase 6 as deferred until more than one project demonstrates real shared pressure for semantic canonicalization.

---

## Practical operating rule for future roadmap updates

When a phase implementation lands:
1. update this roadmap,
2. update the primary SSoT for the affected concept,
3. update `CURRENT_STATE.md`,
4. add a changelog entry,
5. update `coverage-matrix.md` only if concept ownership or doc placement changed,
6. update implementation/reference docs if usage guidance or onboarding guidance changed.

This keeps roadmap status aligned with the governed documentation system without requiring every document to be touched on every change.