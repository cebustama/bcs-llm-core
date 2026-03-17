# Prompt Builder Implementation Guide

**Status:** Active reference  
**Doc Type:** Reference / Implementation Guide  
**Authority:** Secondary  
**Date:** 2026-03-17

## Current alignment notes
This document is implementation guidance, not the primary truth for package semantics.

Before using it, also check:
- `../SSoT_Runtime_and_OpenAI_Provider.md`
- `../SSoT_CONTRACTS.md`
- `../SSoT_Editor_Tooling_and_Wizard.md` when the builder is being used from editor tooling

The most important onboarding rule remains:

**a minimal builder is still valid.**

You do **not** need to adopt:
- contract hints,
- validators,
- repair hints,
- retry classification,
- or orchestration layers

just to create an agent that sends prompts and receives responses.

Those are optional extension points layered on top of the minimal builder path.

---

## 1) Read this first: choose the smallest path that works

There are two common starting points:

### A. New project / new agent
Start with the **minimal builder path**:
- one input DTO,
- one `IPromptBuilder<TInput>` implementation,
- one `PromptBuildResult`,
- separate execution through `PromptExecutionHelper` or equivalent.

Do **not** add later layers unless the workflow actually needs them.

### B. Existing project / legacy builder
Start with an **adapter migration path**:
- keep the legacy builder as the source of truth,
- wrap it in an adapter that implements `IPromptBuilder<TInput>`,
- map legacy outputs into `PromptBuildResult`,
- A/B compare `InstructionsText` and `UserPromptText`,
- keep send blocked on mismatch until parity is confirmed.

Do **not** rewrite the old project into every optional LLM Core feature on day one.

### Quick decision rule
Use the smallest tier that solves the real problem:
- need prompt composition only → minimal builder
- duplicated anti-drift metadata → contract hints
- reusable structured-output validation → validation / repair
- reusable post-validation retry classification → retry layer
- reusable multi-step attempt coordination → orchestration

---

## 2) What problem the Prompt Builder solves
The Prompt Builder pattern exists to separate:
- **agent-specific prompt composition**
from
- **provider request execution**.

That gives you a reusable place to define:
- instruction text,
- user prompt text,
- build modes,
- retry-oriented prompt variants,
- optional logical artifact context,
- optional contract-aware anti-drift metadata,
without forcing those concerns into `ILLMClient` or provider-specific request DTOs.

It does **not** force you to adopt the full lifecycle stack from day one.

---

## 3) Minimal path from scratch
This is the default recommendation for a new project or a simple new agent.

### 3.1 Minimal file set
A minimal builder usually needs only:
- one input DTO,
- one builder class implementing `IPromptBuilder<TInput>`,
- one call site that executes the built prompt.

Typical shared surfaces already available:
- `IPromptBuilder<TInput>`
- `PromptBuildContext`
- `PromptBuildResult`
- `PromptBuildMode`
- `PromptExecutionHelper`
- `PromptExecutionOptions`

### 3.2 Minimal recipe

#### Step 1 — Define an input DTO
Your input DTO should contain only what the agent actually needs to build the prompt.

```csharp
public sealed class ExamplePromptBuildInput
{
    public string Topic;
    public string Audience;
    public PromptBuildMode Mode = PromptBuildMode.Default;
}
```

#### Step 2 — Implement `IPromptBuilder<TInput>`

```csharp
public sealed class ExamplePromptBuilder : IPromptBuilder<ExamplePromptBuildInput>
{
    public PromptBuildResult Build(ExamplePromptBuildInput input)
    {
        var instructions =
            "You are an assistant that writes concise structured summaries.";

        var user =
            $"Write a summary about '{input.Topic}' for audience '{input.Audience}'.";

        return new PromptBuildResult
        {
            InstructionsText = instructions,
            UserPromptText = user,
            Mode = input.Mode
        };
    }
}
```

#### Step 3 — Keep the output logical
The builder should return logical prompt text, not provider request objects.

Good:
- `InstructionsText`
- `UserPromptText`

Not good:
- `OpenAIResponsesRequest`
- provider `file_id`
- endpoint-specific fields

#### Step 4 — Execute separately

```csharp
var build = builder.Build(input);
var result = await PromptExecutionHelper.ExecuteAsync(
    client,
    build,
    new PromptExecutionOptions
    {
        IncludeConversationHistoryInRequest = false,
        MergeNewTurnBackWhenHistorySuppressed = false
    });
```

That is enough for a minimal valid agent.

### 3.3 Example: simple builder with no retry, no contract hints, no validators

```csharp
public sealed class SceneCommentaryPromptBuildInput
{
    public string SceneText;
    public PromptBuildMode Mode = PromptBuildMode.Default;
}

public sealed class SceneCommentaryPromptBuilder
    : IPromptBuilder<SceneCommentaryPromptBuildInput>
{
    public PromptBuildResult Build(SceneCommentaryPromptBuildInput input)
    {
        return new PromptBuildResult
        {
            InstructionsText =
                "You are a narrative assistant. Respond with concise, grounded observations.",
            UserPromptText =
                $"Comment on the following scene:\n\n{input.SceneText}",
            Mode = input.Mode
        };
    }
}
```

Use this when:
- there is one straightforward request shape,
- there is no targeted retry path yet,
- there is no need for anti-drift scaffolding yet,
- you only need standard prompt construction.

This is a complete and acceptable integration path.

---

## 4) Existing project migration quickstart
This is the safest path when the project already has prompt-building code.

### 4.1 Goal
Adopt LLM Core with the **smallest behavior change possible**.

### 4.2 Recommended sequence
1. Keep the legacy builder intact.
2. Create an adapter implementing `IPromptBuilder<TInput>`.
3. Map legacy output into `PromptBuildResult`.
4. Compare only:
   - `InstructionsText`
   - `UserPromptText`
5. Block send on mismatch during migration.
6. Move optional layers only after prompt parity is confirmed.

### 4.3 Why this order matters
It keeps migration noise low.

Do **not** make these changes all at once:
- prompt rewrite,
- contract hints,
- validation migration,
- retry migration,
- orchestration migration.

First prove prompt parity. Then add optional layers incrementally.

### 4.4 Migration pattern from a legacy builder
When migrating an older project-specific builder, the safest approach is often an **adapter**.

#### Why use an adapter first
An adapter lets you:
- keep the old prompt text intact,
- expose the new reusable interface,
- compare outputs before changing behavior.

#### Pattern
- wrap the old builder,
- map legacy outputs into `PromptBuildResult`,
- keep the legacy builder as the source of truth during migration,
- A/B compare prompt text before sending.

#### Recommended A/B rule
Compare:
- `InstructionsText`
- `UserPromptText`

Do **not** use model-output comparison as the primary migration check.

Why:
- it is noisier,
- it costs requests,
- it can differ even when the prompt is identical.

### 4.5 Migration notes for later optional layers
If the migration also introduces contract-aware prompt hints:
- keep legacy wording intact as long as possible,
- first move repeated anti-drift metadata into the hint path,
- keep domain-specific mapping guidance in the builder,
- then A/B compare prompt text again before broadening the refactor.

If the migration also introduces shared validation / repair surfaces:
- keep existing validation logic intact,
- map existing workflow artifacts into shared validation results through adapters,
- prefer additive mirroring before replacing existing editor/runtime workflow gates.

If the migration later introduces orchestration:
- first adopt a narrow real subflow,
- do not replace the whole project workflow at once,
- keep domain-specific apply / merge / candidate logic outside shared core unless repetition is proven.

---

## 5) Boundary map

### 5.1 What belongs in a Prompt Builder
A builder should own:
- agent-specific input interpretation,
- construction of `InstructionsText`,
- construction of `UserPromptText`,
- build-mode branching,
- optional retry context usage,
- optional logical artifact hints,
- optional consumption of prebuilt contract hints.

### 5.2 What does **not** belong in a Prompt Builder
A builder should **not** own:
- `file_id` upload or attachment,
- provider request DTO construction,
- HTTP calls,
- history-suppression policy,
- editor-only busy-state/UI logic,
- domain-independent runtime client creation,
- mandatory validation/repair plumbing for every agent,
- schema/DTO/enums as an implied new source of truth for domain semantics.

### 5.3 What belongs in execution helpers
Execution helpers may own:
- history inclusion/suppression policy,
- routing to `ILLMResponsesFileClient` when file IDs exist,
- fallback to text-only execution.

### 5.4 What belongs in the provider layer
The provider layer owns:
- actual request schema,
- API-variant branching,
- response parsing,
- token usage capture,
- local-history mutation on successful requests.

### 5.5 What belongs in contract-hint providers
Contract-hint providers may own:
- mapping from schema/DTO/enums or equivalent authority sources into `PromptContractHint`,
- normalization of object/field/token-set metadata used for anti-drift prompt scaffolding,
- field/token/fixed-value metadata needed by the builder.

They should **not** own:
- provider request generation,
- editor-only policy,
- domain validation,
- semantic canonicalization,
- or the final wording/staging strategy of the builder.

### 5.6 What belongs in validation / repair layers
Validation / repair layers may own:
- reusable validation result shaping,
- reusable severity / targeting semantics,
- optional mapping from validation state into repair guidance.

They should **not** become mandatory for a minimal builder, and they should **not** own:
- provider execution,
- deterministic domain autofix logic,
- project-specific apply/block policy,
- project-specific retry orchestration.

### 5.7 What belongs in orchestration
Orchestration should stay narrow.

It may own:
- linear attempt-state coordination,
- ordered workflow-step execution,
- bounded re-entry,
- shared step/result control flow.

It should **not** own:
- provider transport,
- project-specific payload reconstruction,
- apply/upsert,
- candidate scoring,
- semantic merge/canonicalization,
- project-specific editor UX.

---

## 6) Optional layers: when to add each one
Only add later layers when they solve a real problem.

The usual progression is:

**minimal builder first → optional contract hints → optional validation/repair → optional retry classification → optional orchestration**

### 6.1 Contract-aware prompt hints
Add `PromptContractHint` / `IContractHintProvider<TInput>` when anti-drift metadata is being duplicated.

Use it when:
- the prompt needs anti-drift scaffolding such as legal field lists,
- the prompt needs exact token sets,
- the prompt needs fixed-value or hard-rule reminders,
- the authority source for that data already exists elsewhere.

Do **not** turn the builder into a generic schema renderer.

The builder should still own:
- wording,
- emphasis,
- sequencing,
- mode-specific framing,
- domain-specific narrative around the contract.

The hint exists to remove duplicated anti-drift metadata, not to replace agent-specific prompt authorship.

Optional provider surface:

```csharp
public interface IContractHintProvider<in TInput>
{
    PromptContractHint BuildContractHint(TInput input);
}
```

### 6.2 Logical artifact hints
Use `PromptArtifactHint` when the builder should know that an external artifact exists, but should not know any provider file IDs.

```csharp
return new PromptBuildResult
{
    InstructionsText = "Use the attached PDF as the authoritative source.",
    UserPromptText = "Extract the requested fields.",
    ArtifactHints = new[]
    {
        new PromptArtifactHint
        {
            Kind = "pdf",
            DisplayName = "Guidebook.pdf",
            Purpose = "authoritative_source"
        }
    }
};
```

This is appropriate when:
- the prompt should mention the artifact conceptually,
- actual `file_id` handling happens later in the executor/provider path.

### 6.3 Validation / repair
Only add this layer when the agent has a real structured-output validation problem.

Minimal idea:
- the builder builds the prompt,
- execution happens separately,
- a validator inspects the returned artifact,
- an optional repair-hint provider can generate guidance,
- later retry/orchestration may consume that guidance.

Typical shared validation surfaces include:
- `ValidationSeverity`
- `ValidationTarget`
- `ValidationIssue`
- `ValidationResult`
- `IResponseValidator<T>`
- `RepairHint`
- `IRepairHintProvider<T>`

Important rule:
this layer is optional and additive.

It should **not** force every new agent to implement:
- structured-output validation,
- repair hints,
- retry classification,
- orchestration.

### 6.4 Retry-aware flow
Use retry classification only when post-validation retry behavior is genuinely reusable.

Shared retry surfaces may include:
- `RetryDisposition`
- `RetryDirective`
- `IRetryClassifier<TContext>`

Builders still do:
- consume `PromptBuildMode`,
- consume `PromptBuildContext.Retry` when wording changes materially,
- build `InstructionsText` and `UserPromptText`.

Shared retry directives do **not** replace:
- your agent-specific input DTO,
- targeted retry payload reconstruction,
- project-specific retry orchestration or candidate application policy.

Recommended pattern:
1. validate / inspect a returned artifact,
2. classify retryability into one or more `RetryDirective` results,
3. in project/editor/runtime bridge code, translate the directive into your local input DTO + `PromptBuildContext`,
4. call the builder again with `Default` or `TargetedRetry` as appropriate.

### 6.5 Orchestrated multi-step flow
Use orchestration only when a real multi-step workflow benefits from shared attempt-state coordination.

Good reasons to add it:
- the project now has multiple steps with explicit attempt state,
- re-entry needs bounded coordination,
- build / execute / validate / retry classify is becoming repetitive.

Do **not** add orchestration just because it exists.

The minimal builder path remains valid even after later tiers are available.

---

## 7) Editor integration pattern
If the builder is used from an `EditorWindow`:
- keep backend selection in the UI if you need migration safety,
- compare legacy vs adapter prompt text before use,
- block send on mismatch during migration,
- keep history/file behavior in execution helpers, not in the builder,
- keep validation mirroring additive before replacing existing project-owned workflow gates,
- keep retry classification outside the builder,
- translate any shared `RetryDirective` back into local input DTO + `PromptBuildContext` in editor/project bridge code rather than inside shared core.

A good migration setup usually has:
- backend dropdown (`Legacy` / `Adapter`),
- compare toggle,
- block-on-mismatch toggle,
- compare button,
- status label showing `A/B PASS` or `A/B FAIL`.

---

## 8) Progressive adoption checklist for a new agent

### Tier 1 — Minimal builder
- [ ] Input DTO is explicit and agent-specific
- [ ] Builder implements `IPromptBuilder<TInput>`
- [ ] Builder returns `InstructionsText` + `UserPromptText`
- [ ] Build mode is explicit when needed
- [ ] Builder returns logical prompt output only
- [ ] Execution stays outside the builder

### Tier 2 — Contract hints (optional)
- [ ] Contract hints remove real anti-drift duplication
- [ ] Contract-hint generation stays agent-owned
- [ ] The builder still owns wording and does not become a schema renderer

### Tier 3 — Validation / repair (optional)
- [ ] Shared validation surfaces solve a real structured-output problem
- [ ] Validators adapt existing workflow artifacts instead of replacing domain semantics prematurely
- [ ] Repair hints remain optional and composable

### Tier 4 — Retry-aware flow (optional)
- [ ] Retry classification is only added when post-validation retry behavior is genuinely reusable
- [ ] Retry directives remain separate from domain-shaped payload reconstruction

### Tier 5 — Orchestrated multi-step flow (optional)
- [ ] Use orchestration only when a real multi-step workflow benefits from shared attempt-state coordination
- [ ] Keep orchestration narrow and evidence-driven
- [ ] Do not let orchestration absorb apply/upsert, candidate scoring, or workflow-engine semantics

---

## 9) Common mistakes
- Letting the builder create provider request DTOs directly
- Passing `file_id` through the builder input for convenience
- Moving history policy into prompt-composition code
- Treating `InstructionsText` as if it always means one specific provider field
- Assuming every agent must implement validators or repair hints before it can be useful
- Over-generalizing domain-specific schema logic into the shared builder layer too early
- Letting contract hints drift into domain validation or canonicalization logic
- Letting validation surfaces drift into deterministic autofix or project-specific workflow policy
- Treating contract hints as if they replace agent-specific prompt authorship
- Treating orchestration as a required default instead of a later optional layer

---

## 10) When **not** to extract a shared builder pattern further
Do **not** generalize more just because one project can use it.

Only extract more shared surfaces when:
- at least one additional agent shows the same pressure,
- the boundary is stable,
- the new abstraction removes duplication without smuggling domain-specific semantics into core.

That is especially important for:
- contract hint providers,
- validator/repair hooks,
- retry classification layers,
- orchestration layers,
- semantic canonicalization layers.

---

## 11) Quick reference summary
If you only remember four rules, remember these:

1. A minimal builder is still a valid integration.
2. Execution stays outside the builder.
3. Migrate existing projects through an adapter first.
4. Add later layers only when they solve a real repeated problem.
