# Current State — BCS / Eon LLM Core

**Status date:** 2026-03-16

## Current true state
- The package has a provider-agnostic client/config core plus an OpenAI provider implementation.
- The editor wizard is the canonical local test harness for agent selection, rebuild, ping, prompt sending, usage display, optional cost estimate, history controls, and PDF upload flow.
- File upload and Responses-with-files exist as explicit optional capabilities in code.
- The pricing pipeline exists as a real subsystem: catalog asset, estimation utility, default seeding helper, and editor menu action.
- A lightweight SSoT documentation baseline is now in place for the package.
- The recent fix batch aligned several previously documented deltas with implemented behavior.

## Active milestone
Stabilize the new documentation baseline and keep it aligned with the implemented runtime/editor behavior after each meaningful change.

## Recently completed
- `allowOsEnvFallback` was wired into the loader behavior when `LLMEnvSettings` exists.
- Backward-compatible permissive OS-env fallback was preserved when no settings asset exists.
- `LLMClientData.ToString()` was changed to redact the API key instead of exposing it.
- The wizard’s file-attach path was clarified and documented as:
  - explicit capability interface first,
  - reflection only as compatibility fallback.
- Reference docs and manual tests were updated to match the current implementation.

## Short horizon
- Apply the updated SSoT text to the governed documentation set.
- Align any remaining `v0` / `v0.1` labels with the package versioning strategy.
- Keep the integration guide and manual test cases synchronized with runtime/editor behavior.

## Medium horizon
- Add debug request diagnostics in editor tooling.
- Improve files panel UX and status feedback.
- Re-evaluate whether reflection fallback is still needed once older/custom integrations are migrated.

## Long horizon
- Expand provider support only when there is real code pressure to do so.
- Add more subsystem docs only if the package surface genuinely grows.

## Known deltas / watch items
- Package metadata says `1.0.0`, while some docs/UI still use `v0` / `v0.1` language. This is now the main visible naming/maturity-label mismatch to clean up.
- No critical correctness drift is currently known in the env-fallback or API-key-redaction areas; those were resolved in the recent fix batch.

## Next edit points
- `SSoT_Runtime_and_OpenAI_Provider.md`
- `SSoT_Editor_Tooling_and_Wizard.md`
- `SSoT_CONTRACTS.md`
- `changelog-ssot.md`
- `SSoT_INDEX.md` or `Documentation~/README.md` for the explicit documentation-update protocol note
