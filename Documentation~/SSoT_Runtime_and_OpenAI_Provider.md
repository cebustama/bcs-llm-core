# SSoT — Runtime and OpenAI Provider

**Status:** Active  
**Authority:** Primary for runtime/client/provider semantics  
**Date:** 2026-03-29

## Scope
This document defines the implemented truth for:
- runtime client abstractions,
- prompt-composition/runtime-execution/orchestration boundaries,
- validation / repair / retry / orchestration runtime boundaries,
- client config asset boundaries,
- OpenAI provider behavior,
- environment/config resolution as consumed by the provider,
- optional file capability surfaces.

## 1) Runtime responsibility split

### 1.1 `LLMClientData`
`LLMClientData` is the provider-agnostic ScriptableObject base for:
- sampling and output-limit parameters,
- stop sequences,
- baseline system instructions,
- per-client fallback pricing fields,
- provider identity and model/base-url abstraction.

It is not the place for provider-specific endpoint semantics beyond abstract properties.

### 1.2 `OpenAIClientData`
`OpenAIClientData` specializes `LLMClientData` with:
- the OpenAI API-variant choice,
- a stable enum-backed model selector,
- an explicit compatibility-vs-current model selection policy,
- env/settings-backed resolution of base URL and endpoints.

### 1.3 `LLMAgentData`
`LLMAgentData` composes:
- agent identity fields,
- an optional dedicated instructions asset,
- a client config asset reference,
- default upload purpose for editor file upload,
- placeholders for initial state/history.

`LLMAgentData` is composition/orchestration data, not the provider implementation.

### 1.4 Prompt composition layer
The package now has a runtime-facing prompt-composition layer intended to sit **before** provider execution.

Current baseline prompt-composition surfaces are:
- `IPromptBuilder<TInput>`
- `PromptBuildMode`
- `PromptBuildContext`
- `PromptBuildResult`
- expanded `PromptContractHint`
- structured contract hint helper types
- optional `IContractHintProvider<TInput>`

Purpose:
- standardize prompt construction,
- normalize `InstructionsText` + `UserPromptText` as the logical output of prompt building,
- support mode-specific builds (`Default`, `Batch`, `TargetedRetry`),
- support optional retry context,
- support optional logical artifact context,
- support optional contract-aware anti-drift metadata,
- keep provider request generation and file capability handling outside the builder.

This layer is runtime-facing infrastructure, not editor-only tooling.

### 1.5 Prompt execution layer
The package also has a small runtime-side prompt execution layer.

Current execution surfaces are:
- `PromptExecutionOptions`
- `PromptExecutionHelper`

Purpose:
- execute a built logical prompt payload over `ILLMClient`,
- optionally suppress request-history inclusion and restore local history around the call,
- optionally merge the resulting turn back after history-suppressed calls,
- prefer file-capable execution surfaces when file IDs are supplied,
- fall back to text-only execution when file capability is unavailable.

This layer is execution reuse, not provider replacement.

### 1.6 Validation / repair extension layer
Phase 3 added a reusable validation / repair extension surface adjacent to prompt composition and execution.

Current validation / repair surfaces are:
- `ValidationSeverity`
- `ValidationTargetScope`
- `ValidationTarget`
- `ValidationIssue`
- `ValidationResult`
- `IResponseValidator<T>`
- `RepairHint`
- optional `IRepairHintProvider<T>`

Purpose:
- standardize how runtime-adjacent workflows describe validation problems,
- preserve issue severity and targeting without forcing project/domain semantics into provider code,
- provide an optional shared repair-guidance seam,
- prepare for retry-oriented execution surfaces without collapsing retry policy into the validator itself.

This layer is reusable runtime-adjacent infrastructure, but it is **optional** for agent adoption. It does not make validation mandatory for every new prompt builder.

### 1.7 Orchestration layer
Phase 5 added a minimal reusable orchestration layer adjacent to prompt execution, validation, and retry classification.

Current orchestration surfaces are:
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
- coordinate a small linear attempt pipeline,
- keep build / execute / validate / classify-retry composable,
- support bounded re-entry through an agent-owned/project-owned adapter,
- avoid turning LLM Core into a branching workflow engine.

This layer is optional runtime-facing coordination infrastructure, not editor-only tooling and not provider transport.

## 2) Base runtime contract

### 2.1 `ILLMClient`
`ILLMClient` is the current provider-agnostic base contract. It includes:
- general model/sampling parameters,
- system instruction storage,
- per-client fallback pricing fields,
- local conversation-history storage,
- text-only completion entrypoints.

It does **not** include file upload or file attachment methods.

### 2.2 Optional capability interfaces
Two optional capability interfaces currently exist:
- `ILLMFileClient` for provider-side file upload,
- `ILLMResponsesFileClient` for Responses requests that include file IDs.

These capabilities are additive and do not change the base contract shape.

### 2.3 Prompt builder boundary relative to `ILLMClient`
`IPromptBuilder<TInput>` does **not** replace `ILLMClient`.

The boundary is:
- Prompt builders produce a logical prompt payload.
- Runtime clients execute provider requests.
- Optional capability interfaces handle upload / file-attachment features.
- Optional contract-hint providers derive prompt metadata from agent/domain authority sources.
- Optional validators and repair-hint providers describe validation outcomes around workflow artifacts when a project needs them.

That means:
- prompt composition remains separate from provider execution,
- the base runtime client contract does not grow just because prompt-building became reusable,
- file attachment remains an execution concern, not a prompt-builder concern,
- contract-hint generation remains agent/domain-owned, not provider-owned,
- validation / repair extensions remain separate from the provider layer.

### 2.4 `PromptExecutionHelper`
`PromptExecutionHelper` is a runtime-side convenience layer for executing a `PromptBuildResult` on top of `ILLMClient`.

Current role:
- accept `InstructionsText` + `UserPromptText`,
- optionally suppress request-history inclusion by snapshotting and restoring `ClientConversationHistory`,
- optionally merge the new turn back after a history-suppressed request,
- prefer `ILLMResponsesFileClient` when file IDs are supplied,
- fall back to text-only execution when file capability is unavailable.

It exists to reuse the history/file execution pattern without redefining `ILLMClient`.

Phase 2 did **not** expand `PromptExecutionHelper` into contract generation, provider DTO shaping, or domain validation.
Phase 3 did **not** expand it into validation execution, repair generation, or retry classification. Those remain separate concerns.
Phase 4 did **not** expand it into retry orchestration, retry loops, or project-specific retry-bridge logic. Those remained separate concerns, and Phase 5 introduced a distinct orchestration layer rather than changing `PromptExecutionHelper` itself.

### 2.5 Minimal adoption path remains valid
A new agent integration may still choose the minimal path:
- build a prompt with `IPromptBuilder<TInput>`,
- execute it through `PromptExecutionHelper` or equivalent runtime code,
- consume the provider result directly.

That minimal path does **not** require:
- contract hints,
- validators,
- repair hints,
- retry classification,
- orchestration surfaces.

Those are additive capabilities used only when the agent/workflow actually needs them.

## 3) Client creation
`LLMClientFactory` currently supports OpenAI and returns `OpenAILLMClient` when the provider is OpenAI. Unsupported providers log an error and return null.

## 4) OpenAI provider behavior

### 4.1 Construction
`OpenAILLMClient` copies the relevant runtime parameters from `OpenAIClientData`, resolves base URL and endpoints, reads the API key from env-backed config, initializes the HTTP client, and copies pricing fields into the runtime instance.

### 4.2 API variants
The provider supports two text-generation request paths:
- **Chat Completions**
- **Responses**

The active path is selected by `OpenAIClientData.ApiVariant`.

### 4.2.1 Model selector policy
`OpenAIClientData` may intentionally keep a mix of:
- current / recommended model IDs,
- compatibility IDs still needed to preserve older serialized assets,
- and optional specialized IDs used only in some workflows.

Current implementation guidance:
- keep legacy enum members in place when changing the selector so Unity asset serialization does not shift unexpectedly,
- prefer current general-purpose IDs such as `gpt-5.4`, `gpt-5.4-mini`, `gpt-5.4-nano`, and `gpt-5.2` for new text-first configs,
- treat `gpt-5-mini` / `gpt-5-nano` as distinct IDs from `gpt-5.4-mini` / `gpt-5.4-nano`,
- keep older expensive / superseded IDs only when there is a real compatibility, reproducibility, or migration reason.

The runtime/provider layer does not itself decide which models should be shown prominently in editor UX. That presentation policy remains editor-owned.

### 4.3 Chat Completions path
The Chat Completions request path:
- builds a message list with instructions + prompt + history,
- sends `frequency_penalty` and stop sequences when available,
- parses prompt/completion token usage,
- appends user and assistant turns to local history on successful responses.

### 4.4 Responses path
The Responses text-only request path:
- builds a message list from current local history plus the new prompt,
- passes instructions separately,
- avoids request fields known to cause schema issues in the current request shape,
- parses input/output/reasoning token usage,
- appends user and assistant turns to local history on successful responses.

### 4.5 Why `InstructionsText` is the canonical builder term
The prompt-composition layer uses `InstructionsText` rather than a hard requirement such as `SystemPromptText`.

Reason:
- Chat Completions materializes instructions as a `system` message.
- Responses materializes instructions as a top-level `instructions` field.

So `InstructionsText` is the package-level logical term, while provider-specific request shapes remain downstream concerns.

### 4.6 Request history inclusion
The runtime client owns the local history store. It does not expose a first-class “include history in request” flag on the base contract. Callers can influence outgoing context by manipulating `ClientConversationHistory` around the call.

That means:
- history storage is runtime state,
- history inclusion is caller policy,
- prompt builders do not own this policy.

## 5) File upload and file attachment

### 5.1 Upload
`OpenAILLMClient` implements `ILLMFileClient.UploadFileAsync(...)`.
Current implementation characteristics:
- intended for editor/tooling workflows,
- currently PDF-oriented in practice,
- returns `FileId`, `Filename`, and `Bytes`.

### 5.2 File attachment in requests
`OpenAILLMClient` also implements `ILLMResponsesFileClient`.
Current characteristics:
- attachment is modeled as a Responses-only feature,
- when no file IDs are provided, behavior falls back to the normal text-only path,
- when file IDs are provided but the API variant is not Responses, the provider warns and falls back to text-only behavior.

### 5.3 Logical artifact hints vs provider file IDs
Prompt composition may carry descriptive artifact context through `PromptArtifactHint`.

That is **not** the same as provider file IDs:
- `PromptArtifactHint` is logical/descriptive context for prompt construction.
- `file_id` is provider-specific execution data for a request payload.

The builder may describe artifacts; the executor/provider layer resolves and attaches actual file IDs.

### 5.4 Serialization note
The file-attachment request shape depends on null fields being ignored so that file-part DTOs do not serialize unsupported text fields into file input parts.

## 6) History mutation on success/failure
Current implemented behavior:
- successful requests append user + assistant turns to local history,
- failure paths return an empty result and do not establish a successful turn,
- callers that temporarily suppress history for a request must restore state themselves if they want continuity.

## 7) Env / settings resolution as consumed by runtime

### 7.1 Path resolution order
The env loader currently resolves `.env` source in this order:
1. `LLM_ENV_PATH` or legacy `EON_ENV_PATH`,
2. `LLMEnvSettings.envFilePath` when auto-load is enabled,
3. default project-root `.env`.

### 7.2 Value resolution
Current OpenAI config behavior:
- `OPENAI_API_KEY` is read from env via `LLMEnvLoader.Get(...)`.
- Base URL and endpoint values are resolved from env first, then `LLMEnvSettings`, then hard-coded defaults.

### 7.3 OS-env fallback behavior
Current behavior is now explicit:
- if `LLMEnvSettings` exists, `allowOsEnvFallback` governs whether missing keys may fall back to OS environment variables;
- if no settings asset exists, permissive OS-env fallback remains enabled for backward compatibility.

That means the setting is now a real runtime gate when settings are present, not just a UI placeholder.

## 8) Pricing values in runtime
Runtime clients carry fallback pricing fields copied from `LLMClientData`. These are not the preferred authoritative source for tooling when a central catalog is available, but they are the implemented fallback path.

## 9) Current adoption state of the Prompt Builder + validation / retry baseline
Current documented Phase 1 + Phase 2 + Phase 3 + Phase 4 + Phase 5 state:
- the reusable prompt-composition surfaces exist,
- the first proven consumer is an agent-specific adapter around APEC Watch prompt building,
- A/B comparison at prompt-text level was used to verify non-regression,
- contract-aware prompt hints are now operational in the build path rather than only conceptual,
- the first proven contract-hint provider pattern is an agent-owned APEC Watch mapping from schema/DTO/enums into `PromptContractHint`,
- the shared validation / repair extension surface now exists,
- APEC Watch can map raw input, bundle-parse output, and persisted database validation into the shared validation layer,
- the validation / repair layer is additive and does not make the simple builder → execute path obsolete,
- the shared retry-classification surface now exists,
- APEC Watch maps pack-targeted retry, rule-targeted retry, and terminal confirmed-null states into local retry-classification context and reusable retry directives,
- `ValidationTarget` is reused for retry target/scope description,
- `PromptExecutionHelper` remains a single-shot execution helper rather than a retry orchestrator,
- retry-directive → prompt-input reconstruction remains local bridge/orchestration code rather than a shared runtime contract,
- a minimal orchestration layer now exists in shared runtime code,
- the first proven adoption is a narrow APEC Watch single-PDF-extraction pilot,
- and wider extraction beyond that pilot remains evidence-driven rather than automatic.

## 10) Known implementation-sensitive areas
- Endpoint/base-url edits require client rebuild in editor workflows.
- The provider contract is explicit for file capabilities, but editor tooling still keeps a reflection fallback path for compatibility.
- `LLMClientData.ToString()` now redacts the API key; this should remain true and should not be regressed casually.
- Prompt builders should stay small and focused on prompt composition; avoid drifting provider file IDs, history policy, or domain validation back into the builder surface without a stronger cross-project need.
- Contract hints should stay descriptive and runtime-facing. Do not let builders or shared runtime layers absorb project-specific schema semantics just because the first agent needed anti-drift scaffolding.
- `IContractHintProvider<TInput>` is currently a valid shared pattern, but it should remain evidence-driven rather than becoming mandatory everywhere by default.
- Validation / repair surfaces should stay descriptive and composable. Do not let them silently absorb project-specific canonicalization, deterministic autofix behavior, or retry execution policy.
- Retry-classification surfaces should stay narrow. Do not let them silently absorb provider transport retry policy or domain-shaped prompt-input reconstruction.
- Orchestration surfaces should stay narrow. Do not let them silently absorb apply/upsert, candidate scoring, persisted DB remediation, branching workflow-engine semantics, or editor-only UX/state.
- Minimal agent integrations must remain lightweight. Do not make validation, repair hints, or retry classification mandatory for simple prompt-builder use cases.

## 11) Not authoritative here
This document does not define:
- wizard UX sequencing,
- editor panel behavior,
- manual regression test steps,
- pricing catalog maintenance procedure,
- reference-style step-by-step builder tutorials,
- project-specific retry families or retry heuristics.
