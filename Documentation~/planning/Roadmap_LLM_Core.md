# Roadmap — LLM Core

**Status:** Active  
**Date:** 2026-03-16

## Goal
Keep the package small, explicit, and low-drift while making the current OpenAI/editor workflow easier to maintain and reason about.

## Milestone 1 — Documentation governance baseline
### Outcome
A stable, lightweight SSoT documentation system replaces the previous flat mixed set.

### DoD
- `Documentation~/` exists with governance core docs.
- Runtime/editor/pricing subsystem truths each have a primary home.
- Previous mixed state-plan doc is archived.
- Reference docs no longer pretend to be authoritative.

## Milestone 2 — Remove behavior/documentation ambiguity
### Outcome
Small but important mismatches are made explicit or resolved.

### Target items
- Decide the fate of reflection fallback for Responses-with-files:
  - keep it as compatibility only, or
  - remove it and rely purely on the capability interface.
- Align docs/UI version labels with package versioning.
- Either wire `allowOsEnvFallback` into the loader or remove/deprecate the setting.
- Redact API key from `LLMClientData.ToString()` or clearly prevent its casual use in logs.

### DoD
- Chosen direction is reflected in code and docs.
- `CURRENT_STATE.md` no longer lists these as unresolved deltas.

## Milestone 3 — Editor diagnostics and UX polish
### Outcome
The wizard becomes easier to debug and safer to operate.

### Candidate work
- add debug-only request diagnostics,
- expose last request payload in memory for copy/debug,
- improve files panel affordances and status messaging,
- clarify active variant / attach eligibility in UI.

### DoD
- At least one diagnostics path exists,
- file attach state is easier to understand,
- regression notes are updated.

## Milestone 4 — Provider growth only if earned
### Outcome
Additional providers or deeper abstractions are introduced only when supported by real usage pressure.

### Non-goal for now
Do not build speculative provider-generalization layers that the package does not yet need.

## Explicit non-goals
- Turning pricing estimates into billing reconciliation
- Creating a heavyweight research/archive system before the package needs it
- Expanding documentation count without a real authority benefit
