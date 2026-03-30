# SSoT Contracts — BCS / Eon LLM Core

**Status:** Active  
**Authority:** Primary for cross-cutting shared semantics  
**Date:** 2026-03-29 (updated — serializer audit invariant added)

## Purpose
This document stores rules and definitions that span runtime, provider, editor tooling, pricing, reusable prompt composition, and the extracted validation / repair / retry-classification / orchestration extension surfaces.

## Shared terms
- **ClientData**: a ScriptableObject defining provider-independent and provider-specific runtime configuration.
- **AgentData**: a ScriptableObject that composes an instructions asset, client config, upload purpose, and initial state/history placeholders.
- **System instructions**: baseline instruction text attached to the client config.
- **Agent instructions**: instructions text stored in a dedicated instructions asset.
- **Wizard override instructions**: editor-only textual override typed directly in the wizard.
- **InstructionsText**: the package-level logical term for the instruction portion produced by a prompt builder. It is provider-neutral and may become a `system` message or a top-level `instructions` field depending on the downstream API variant.
- **UserPromptText**: the package-level logical term for the user-facing prompt content produced by a prompt builder.
- **Prompt builder**: a reusable component that turns agent-specific input into a logical prompt payload.
- **Prompt build mode**: the declared build intent for a prompt build, currently `Default`, `Batch`, or `TargetedRetry`.
- **Prompt artifact hint**: logical/descriptive artifact context used during prompt construction. It is not a provider `file_id`.
- **Prompt contract hint**: runtime-facing metadata that describes contract shape and anti-drift constraints for prompt construction. It is descriptive prompt metadata, not provider request DTO data and not domain truth by itself.
- **Prompt contract object hint**: a shape-level description of an object used by the contract hint surface.
- **Prompt contract field hint**: a field-level description used inside a contract object hint.
- **Prompt token set hint**: a named set of allowed tokens that can be referenced by fields or rendered into anti-drift prompt scaffolding.
- **Contract hint provider**: an optional reusable pattern that maps agent/domain authority sources into a `PromptContractHint`.
- **Validation severity**: the shared severity surface for validation issues. Current values are `Warning`, `Error`, and `Blocker`.
- **Validation target**: a shared location descriptor for a validation issue. It may point at the whole response, a specific item, or a specific field/path.
- **Validation issue**: a reusable description of a detected problem, including severity, stage, message, and target.
- **Validation result**: the shared aggregated result produced by a response validator.
- **Response validator**: a reusable component that validates one workflow artifact and emits a shared `ValidationResult`.
- **Repair hint**: reusable guidance describing how a caller or later retry layer may attempt recovery after validation.
- **Repair-hint provider**: an optional reusable pattern that maps validated artifacts and issues into repair guidance.
- **Retry disposition**: the shared high-level retry outcome surface. Current values are `NoRetry`, `Retryable`, and `Terminal`.
- **Retry directive**: a reusable description of whether retry should happen, what target it applies to, and which prompt-build mode is suggested for re-entry.
- **Retry classifier**: a reusable component that classifies caller-supplied retry context into one or more `RetryDirective` results.
- **Workflow state**: a typed attempt-state object that accumulates workflow artifacts across build / execute / validate / classify-retry steps.
- **Workflow step**: a reusable unit that reads and/or mutates workflow state and returns a step-level advance result.
- **Workflow re-entry adapter**: an agent/project-owned component that prepares the next attempt after a retryable step result.
- **Linear workflow runner**: the minimal shared coordinator that executes ordered workflow steps and supports bounded re-entry without becoming a workflow engine.
- **Local conversation history**: the client's in-memory `ClientConversationHistory`.
- **Request history inclusion**: whether current local history is sent as context in the outgoing request.
- **File upload**: uploading a local PDF to the provider and receiving a file identifier.
- **File attachment**: including one or more uploaded `file_id`s in a request payload.
- **Pricing catalog**: non-secret ScriptableObject containing per-provider / per-model / per-tier USD-per-1M-token rates.

## Cross-cutting invariants

### 1) Secrets and non-secrets
- Secrets must come from env / OS env, not serialized assets.
- `OPENAI_API_KEY` is a secret.
- Base URL and endpoint defaults are non-secret configuration and may live in settings assets.
- Debug-oriented string representations must redact secret values rather than printing them directly.

### 2) Authority split for instructions
Effective instructions are resolved in this order:
1. wizard override text,
2. agent instructions asset text,
3. client-data `SystemInstructions`,
4. empty string.

This precedence is a package-level behavioral contract for the current editor tooling.

### 3) Prompt composition boundary
- A prompt builder produces logical prompt content, not a provider request DTO.
- The canonical prompt-builder output terms are `InstructionsText` and `UserPromptText`.
- `InstructionsText` / `UserPromptText` are package-level logical names; providers may materialize them differently.
- Prompt builders may use retry context, contract hints, or logical artifact hints when the agent requires them.
- Prompt builders must not own upload, `file_id` resolution, or provider request serialization.
- Prompt builders must not require the validation / repair layer in order to be considered valid package integrations.

### 4) Build-mode semantics
Current shared build modes are:
- `Default`: the standard build path for a single request unit.
- `Batch`: a batch-oriented prompt build path when an agent intentionally constructs a batch request.
- `TargetedRetry`: a retry build for a specific failed unit with recovery context.

A mode is a declared prompt-construction intent, not a provider transport setting.

### 5) History semantics
- Local history storage and request-history inclusion are distinct concepts.
- Runtime clients own their local history store.
- Whether to suppress or include history in the outgoing request is a caller policy, not a change to the base runtime contract.
- Prompt builders do not own request-history inclusion policy.

### 6) Capability interfaces
- `ILLMClient` is the base provider-agnostic contract.
- `ILLMFileClient` is an optional capability for provider-side file upload.
- `ILLMResponsesFileClient` is an optional capability for Responses requests with attached file IDs.
- Optional capabilities must not silently redefine the base `ILLMClient` surface.
- The current preferred integration pattern is:
  - explicit capability interface first,
  - reflection only as editor-side compatibility fallback.

### 7) Prompt artifact hint vs file attachment
- `PromptArtifactHint` is logical/descriptive prompt context.
- Provider `file_id` attachment is execution data.
- These must not be conflated.
- A builder may describe an artifact without knowing any provider file ID.

### 8) Prompt execution helper boundary
A helper such as `PromptExecutionHelper` may:
- apply history-suppression policy around an `ILLMClient` call,
- route to `ILLMResponsesFileClient` when file IDs are provided,
- fall back to text-only execution when file capability is unavailable.

It still does **not** redefine the base runtime contract, and it is distinct from the prompt builder.

### 9) OpenAI variant boundary
- Text-only requests can go through Chat Completions or Responses.
- File attachment is currently modeled as an OpenAI Responses feature, not a generic provider feature.

### 10) Pricing semantics
- Pricing is expressed as USD per 1M tokens.
- Cached input tokens are a subset of input tokens.
- Reasoning tokens may be treated as output tokens for estimation purposes when the caller opts in.
- Estimates are approximate tooling outputs, not billing reconciliation.

### 11) Env-fallback rule
- When `LLMEnvSettings` exists, `allowOsEnvFallback` governs whether missing values may fall back to OS env.
- When no settings asset exists, permissive OS-env fallback remains enabled for backward compatibility.

### 12) Prompt Builder implementation rule
When implementing a new agent-specific builder:
- define an explicit input DTO for that agent,
- implement `IPromptBuilder<TInput>` (or the current local equivalent surface),
- keep domain-specific prompt text inside the agent builder,
- keep provider request generation outside the builder,
- use prompt-text A/B comparison when migrating from a legacy builder.

### 13) Contract-aware prompt hint rule
- `PromptContractHint` is a current runtime-facing prompt-composition surface, not merely a placeholder concept.
- Contract hints describe **shape and anti-drift constraints**, such as:
  - object/field lists,
  - token sets,
  - fixed field values,
  - hard rules,
  - and other prompt-oriented contract metadata.
- Contract hints do **not** by themselves establish domain truth. They are prompt scaffolding derived from domain/project authority sources.
- The preferred pattern is:
  - domain/project authority lives in schema/DTO/enums or equivalent sources,
  - an agent-owned mapping layer converts those sources into `PromptContractHint`,
  - the builder consumes the hint while still owning prompt wording and agent-specific context.
- `IContractHintProvider<TInput>` is a valid reusable pattern for building hints, but it remains optional.
- Contract-hint generation must not be forced into provider code or editor-only code.
- Prompt builders must not turn into generic schema renderers. Builders still own prompt wording, emphasis, and agent-specific instructional framing.
- Domain-specific semantic mapping rules remain project-owned unless stronger cross-project evidence justifies extraction.

### 14) Validation / repair extension rule
- The shared validation / repair layer is adjacent to prompt composition, not inside it.
- `IResponseValidator<T>` validates a workflow artifact and emits a reusable `ValidationResult`.
- Shared validation surfaces describe **what is wrong, where, and how severe it is**. They do not own provider execution and do not silently become retry execution policy.
- The shared minimum severity surface is:
  - `Warning`
  - `Error`
  - `Blocker`
- Shared validation targets must support at least:
  - whole-response targeting,
  - item-level targeting,
  - field/path targeting.
- Validation / repair extraction must not absorb domain-specific business meaning, semantic canonicalization, or project-owned key construction.
- Validators should not be forced to mutate payloads.
- Deterministic normalization / canonicalization / autofix logic remains project-owned unless stronger cross-project evidence appears.
- `IRepairHintProvider<T>` is optional. A valid package integration may use validators without repair hints, or may skip both entirely when the agent does not need them.

### 15) Retry-classification extension rule
- The shared retry layer is adjacent to validation / repair, not inside the prompt builder and not inside provider transport.
- `IRetryClassifier<TContext>` classifies caller-supplied retry context and emits reusable `RetryDirective` results.
- Shared retry surfaces describe **whether retry is appropriate, what the retry target is, and what prompt-build mode is suggested**. They do not own execution loops, provider transport retry policy, or domain-shaped prompt-input reconstruction.
- The shared minimum disposition surface is:
  - `NoRetry`
  - `Retryable`
  - `Terminal`
- Shared retry directives reuse `ValidationTarget` for target/scope description rather than introducing a second target model by default.
- Retry classification may consume validation results, repair hints, project-specific gate evidence, or other caller-owned signals, but the mapping from those signals into retry context remains project/agent-owned unless stronger cross-project evidence appears.
- Targeted retry payload reconstruction remains project-owned. Shared retry contracts must not quietly become a disguised transport for domain DTOs.
- `IRetryClassifier<TContext>` is optional. A valid package integration may use prompt builders without retry classification, or may use retry classification without introducing shared orchestration.

### 15b) Orchestration extension rule
- The shared orchestration layer is adjacent to prompt execution, validation, and retry classification; it is not inside the prompt builder and it is not provider transport.
- Shared orchestration exists to coordinate a **small linear attempt pipeline** such as:
  - build
  - execute
  - validate
  - classify retry
  - optional re-entry
- `PromptExecutionHelper` remains a **single-shot execution helper** and does not silently become the orchestrator.
- Shared orchestration should stay intentionally narrow:
  - typed attempt-state base
  - linear ordered step chain
  - bounded coordinator
  - agent-owned/project-owned re-entry adapter
- Validation may appear at more than one point in the workflow chain; orchestration must not force validation into a single hardcoded slot.
- Shared orchestration must not absorb:
  - provider transport retry policy,
  - domain-shaped retry payload reconstruction,
  - project-specific candidate scoring / replacement,
  - apply/upsert semantics,
  - persisted DB remediation,
  - generic branching/DAG/workflow-engine semantics,
  - or editor-only UX/state.
- Shared orchestration is optional. A valid package integration may use prompt builders, validators, and retry classification without adopting orchestration.

### 16) Minimal adoption rule
A minimal builder integration remains a first-class supported path.

Minimum valid path:
- define an input DTO,
- implement `IPromptBuilder<TInput>`,
- return `InstructionsText` + `UserPromptText`,
- execute via `PromptExecutionHelper` or equivalent runtime code.

That path does **not** require:
- contract hints,
- validation,
- repair hints,
- retry classification,
- orchestration surfaces.

Those are additive capabilities, not mandatory prerequisites for a simple agent integration.

### 17) Serializer pre-migration audit rule
When adopting the Prompt Builder path in an existing project, the request serializer
must be verified for field completeness **before** building any adapter or running any
prompt-parity comparison.

- Serialize a representative domain object with the current serializer.
- Confirm that every expected field — especially collections, dictionaries, and nested
  objects — appears correctly in the output.
- Specific known issue: Unity's `JsonUtility.ToJson` silently drops
  `Dictionary<K,V>` fields and other non-serializable types with no error or warning.
  Any project using `JsonUtility` for domain request serialization must be considered
  at risk until verified.
- If fields are missing, replace or fix the serializer (e.g. switch to
  Newtonsoft `JsonConvert.SerializeObject`) before beginning adapter migration.
- Confirm the field naming convention (camelCase, PascalCase, etc.) in the output
  matches the prompt shape the model expects.

Rationale: if the serializer silently drops fields, the model has been receiving
incomplete context for the entire lifetime of the project. Prompt-parity A/B comparison
is meaningless until the serializer output is correct — you would be comparing two
wrong things against each other.

## Not authoritative here
This document does not define:
- exact provider request DTO shapes,
- exact wizard screen layout,
- pricing catalog maintenance workflow,
- step-by-step reference examples,
- phase-specific retry classification policy,
- phase-specific orchestration policy.
