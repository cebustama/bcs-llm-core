# Migration Report — LLM Core Documentation → SSoT Governance v0.2

**Date:** 2026-03-16

## 1) Current-state diagnosis

### Existing docs and their old roles
- `README.md` acted as the package entry point and partial overview.
- `llm-agent-wizard-state-and-plan.md` acted as a mixed document: current state + subsystem truth + roadmap.
- `LLM_Core_EditorWindow_Integration_Guide.md` acted as reusable integration/reference material.
- `llm-agent-wizard-test-cases.md` acted as validation/reference support.
- `llm-pricing-pipeline.md` was already close to a subsystem doc and has now been promoted into a governed pricing SSoT.

### Main problems found
- authority was implicit rather than explicit,
- one file was doing too many jobs,
- reference material could be mistaken for primary truth,
- there was no coverage matrix or semantic changelog,
- current state and roadmap were not cleanly separated,
- some code/documentation deltas were real and worth naming explicitly.

### Important code-backed truths surfaced during migration
- `ILLMClient` is still the small provider-agnostic base contract.
- File upload and Responses-with-files are explicit optional capability interfaces.
- Wizard history inclusion is editor policy layered over runtime state.
- Effective instructions precedence is explicit in code.
- Env/settings behavior and pricing estimate precedence are now documented explicitly.
- `allowOsEnvFallback` and `LLMClientData.ToString()` were flagged as real cleanup/watch items.

## 2) Target structure

```text
README.md
Documentation~/
  README.md
  SSoT_INDEX.md
  SSoT_CONTRACTS.md
  CURRENT_STATE.md
  coverage-matrix.md
  changelog-ssot.md

  SSoT_Runtime_and_OpenAI_Provider.md
  SSoT_Editor_Tooling_and_Wizard.md
  SSoT_Pricing_Pipeline.md

  planning/
    README.md
    Roadmap_LLM_Core.md

  reference/
    README.md
    LLM_Core_EditorWindow_Integration_Guide.md
    llm-agent-wizard-test-cases.md

  archive/
    README.md
    llm-agent-wizard-state-and-plan.md
    llm-pricing-pipeline.md
```

## 3) File-by-file migration map

| Original file | New classification | Final destination |
|---|---|---|
| `README.md` | keep, rewritten as package overview | root `README.md` |
| `llm-agent-wizard-state-and-plan.md` | split + archive | `Documentation~/archive/` plus content redistributed |
| `LLM_Core_EditorWindow_Integration_Guide.md` | reference | `Documentation~/reference/` |
| `llm-agent-wizard-test-cases.md` | validation/reference | `Documentation~/reference/` |
| `llm-pricing-pipeline.md` | absorbed into subsystem SSoT + archived copy | `Documentation~/SSoT_Pricing_Pipeline.md` + `Documentation~/archive/` |

## 4) README placement recommendation
- Keep one public/package-level `README.md` in the root.
- Add `Documentation~/README.md` as the governed documentation entry point.
- Add short READMEs to `planning/`, `reference/`, and `archive/`.

## 5) Safe replacement guide

### Phase A — Add the governed tree
1. Copy the new `Documentation~/` folder into the package.
2. Replace the root `README.md` with the rewritten overview.

### Phase B — Reclassify old docs
3. Move the mixed wizard state-plan doc into `Documentation~/archive/`.
4. Move the integration guide and manual tests into `Documentation~/reference/`.
5. Replace the standalone pricing explainer with `SSoT_Pricing_Pipeline.md` and keep the old explainer only as archive.

### Phase C — Fix links and habits
6. Update any links that still point to the old flat files.
7. Treat `SSoT_INDEX.md` as the first documentation stop for future edits.
8. Update `CURRENT_STATE.md`, the relevant SSoT, `coverage-matrix.md`, and `changelog-ssot.md` whenever semantics change.

### Phase D — Optional cleanup after docs land
9. Decide whether to remove reflection fallback for Responses-with-files.
10. Resolve `allowOsEnvFallback` drift.
11. Redact secrets from `LLMClientData.ToString()`.
12. Align maturity/version labels.

## 6) Optional future code/documentation requests
The migration is already viable with the material reviewed. If you later want an even tighter second pass, the most useful extra code review would be:
- `LLMClientBase.cs`
- any request/response DTOs specific to OpenAI Responses payload generation
- any additional provider implementations, if they start to exist
- any future diagnostics tooling classes

None of those are required for the documentation set included in this package.
